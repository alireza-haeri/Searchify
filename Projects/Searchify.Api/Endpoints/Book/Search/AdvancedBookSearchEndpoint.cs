using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Mvc;
using Searchify.Api.Common.Helper;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class AdvancedBookSearchEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("search", async (
            ElasticsearchClient client,
            CancellationToken token,
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? isbn,
            [FromQuery] string[]? categories,
            [FromQuery] string? description,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        ) =>
        {
            if (page <= 0)
                return Results.BadRequest("Invalid page number");
            if (pageSize <= 0)
                return Results.BadRequest("Invalid page size");
            if (sortBy != null && sortBy is not ("title" or "author" or "isbn"))
                return Results.BadRequest("Invalid sort field");
            if (sortOrder != null && sortOrder is not ("asc" or "desc"))
                return Results.BadRequest("Invalid sort order");

            var titleField = Infer.Field<BookEntityModel>(b => b.Title);
            var titleKeywordField = Infer.Field<BookEntityModel>(b => b.Title.Suffix("keyword"));
            var authorField = Infer.Field<BookEntityModel>(b => b.Author);
            var authorKeywordField = Infer.Field<BookEntityModel>(b => b.Author.Suffix("keyword"));
            var isbnField = Infer.Field<BookEntityModel>(b => b.ISBN.Suffix("keyword"));
            var descField = Infer.Field<BookEntityModel>(b => b.Description);
            var categoriesKeywordField = Infer.Field<BookEntityModel>(b => b.Categories).Suffix("keyword");
            var ratingField = Infer.Field<BookEntityModel>(b => b.Rating);

            var mustQueries = new List<Query>();

            //Author
            mustQueries.AddMatchIfNotEmpty(authorField, author);
            //Isbn
            mustQueries.AddTermIfNotEmpty(isbnField, isbn);

            //Title
            if (!string.IsNullOrWhiteSpace(title))
                mustQueries.Add(new BoolQuery
                {
                    Should = new List<Query>
                    {
                        new MatchQuery { Field = titleField, Query = title, Boost = 5 },
                        new MatchQuery
                            { Field = titleField, Query = title, Fuzziness = new Fuzziness("AUTO"), Boost = 1 }
                    },
                    MinimumShouldMatch = 1
                });

            //Description
            if (!string.IsNullOrEmpty(description))
                mustQueries.Add(new MatchQuery
                    { Field = descField, Query = description, Fuzziness = new Fuzziness("AUTO"), Boost = 0.5f });

            //Category
            if (categories is { Length: > 0 })
                foreach (var cat in categories)
                {
                    // جستجوی عبارت کامل با Boost بالا
                    mustQueries.Add(new MatchPhraseQuery
                        { Field = (Field)categoriesKeywordField, Query = cat, Boost = 3 });

                    // جستجوی کلمات جداگانه با Operator AND
                    mustQueries.Add(new MatchQuery
                        { Field = (Field)categoriesKeywordField, Query = cat, Operator = Operator.And, Boost = 1 });
                }

            //Sort
            var sortFields = new List<SortOptions>();

            sortFields.Add(new SortOptions(){Field = new FieldSort(){Field = ratingField,Order = SortOrder.Desc}});
            if (string.IsNullOrEmpty(sortBy))
                sortFields.Add(new SortOptions { Score = new ScoreSort { Order = SortOrder.Desc } });
            else
            {
                var sortField = sortBy switch
                {
                    "title" => titleKeywordField, "author" => authorKeywordField, "isbn" => isbnField, _ => null
                };

                if (sortField != null)
                {
                    var order = sortOrder == "desc" ? SortOrder.Desc : SortOrder.Asc;
                    sortFields.Add(new SortOptions { Field = new FieldSort { Field = sortField, Order = order } });
                }
            }

            var response = await client.SearchAsync<BookEntityModel>(a => a
                .Indices(BookEntityModel.IndexName)
                .From((page - 1) * pageSize)
                .Size(pageSize)
                .Query(q => q.Bool(b => b.Must(mustQueries)))
                .Source(s => s.Filter(f => f
                    .Includes(
                        i => i.Title,
                        i => i.Author,
                        i => i.ISBN,
                        i => i.Categories,
                        i => i.Description
                    )
                ))
                .Sort(sortFields), token);

            if (!response.IsValidResponse)
                return Results.Problem($"Search failed",statusCode:StatusCodes.Status500InternalServerError);

            var suggestions = new List<AdvancedBookSearchSuggestionDto>();
            if (response.Total < 5 && !string.IsNullOrWhiteSpace(title))
            {
                var suggestResponse = await client.SearchAsync<BookEntityModel>(s => s
                        .Indices(BookEntityModel.IndexName)
                        .Size(3)
                        .Query(q => q.Bool(b => b
                            .Should(
                                // تطبیق قوی روی Title
                                sh => sh.Match(m => m
                                    .Field(f => f.Title)
                                    .Query(title)
                                    .Boost(5)
                                    .Fuzziness(new Fuzziness("AUTO"))
                                    .MinimumShouldMatch("1")
                                ),
                                // جستجوی پیشوندی روی Title برای عبارت‌های کوتاه
                                sh => sh.MatchPhrasePrefix(mpp => mpp
                                    .Field(f => f.Title)
                                    .Query(title)
                                    .Boost(4)
                                ),
                                // تطبیق روی Description
                                sh => sh.Match(m => m
                                    .Field(f => f.Description)
                                    .Query(title)
                                    .Boost(2)
                                    .Fuzziness(new Fuzziness("AUTO"))
                                ),
                                // تطبیق روی Categories
                                sh => sh.Match(m => m
                                    .Field(f => f.Categories)
                                    .Query(title)
                                    .Boost(1)
                                )
                            )
                            .MinimumShouldMatch(1)
                        )),
                    token
                );

                suggestions = suggestResponse.Documents.Select(d => new AdvancedBookSearchSuggestionDto()
                {
                    Title = d.Title,
                    Author = d.Author,
                    Isbn = d.ISBN
                }).ToList();
            }

            
            var result = new AdvancedBookSearchResponse
            {
                Total = response.Total,
                Suggestions = suggestions,
                Books = response.Documents.Select(b => new AdvancedBookSearchDto
                {
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Categories = b.Categories.ToArray(),
                    Description = b.Description
                }).ToList()
            };

            return Results.Ok(result);
        });
    }


    private class AdvancedBookSearchResponse
    {
        public long Total { get; set; }
        public List<AdvancedBookSearchDto> Books { get; set; } = [];
        public List<AdvancedBookSearchSuggestionDto> Suggestions { get; set; } = [];
    }

    private class AdvancedBookSearchSuggestionDto
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Isbn { get; set; }
    }
    private class AdvancedBookSearchDto
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string[] Categories { get; set; }
        public string Description { get; set; }
    }
}