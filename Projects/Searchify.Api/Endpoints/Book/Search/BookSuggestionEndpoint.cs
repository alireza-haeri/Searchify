using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class BookSuggestionEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("suggestion", async (
            ElasticsearchClient client,
            CancellationToken token,
            [FromQuery] string q) =>
        {
            var titleKeywordField = Infer.Field<BookEntityModel>(b => b.Title.Suffix("keyword"));
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Query parameter 'q' is required.");

            var suggestionResponse = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Size(5)
                    .Query(qu => qu
                        .Bool(b => b
                            .Should(
                                s => s.Match(m => m
                                    .Field(f => f.Title)
                                    .Query(q)
                                    .Boost(5)
                                    .Fuzziness(new Fuzziness("AUTO"))
                                    .MinimumShouldMatch("1")),
                                s => s.MatchPhrasePrefix(mpp => mpp
                                    .Field(f => f.Title)
                                    .Query(q)
                                    .Boost(4)),
                                s => s.Match(m => m
                                    .Field(f => f.Description)
                                    .Query(q)
                                    .Boost(2)
                                    .Fuzziness(new Fuzziness("AUTO"))),
                                s => s.Match(m => m
                                    .Field(f => f.Categories)
                                    .Query(q)
                                    .Boost(1))
                            )
                            .MinimumShouldMatch(1)
                        )
                    )
                    .Source(s => s.Filter(f => f
                        .Includes(i => i.Title, i => i.Author, i => i.Rating)))
                    .Sort(s => s
                        .Score(sc => sc.Order(SortOrder.Desc))
                        .Field(f => f.Rating, SortOrder.Desc)
                        .Field(titleKeywordField, SortOrder.Desc))
                , token);


            if (!suggestionResponse.IsValidResponse)
                return Results.Problem("Search Failed", statusCode: StatusCodes.Status500InternalServerError);

            return Results.Ok(suggestionResponse.Documents.Select(s =>
                    new BookSuggestionResponse(
                        s.Title,
                        s.Author,
                        s.Rating)
                )
            );
        });
    }

    private record BookSuggestionResponse(
        string Title,
        string Author,
        double Rating);
}