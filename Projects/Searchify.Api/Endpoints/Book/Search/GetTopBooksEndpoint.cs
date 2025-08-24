using System.ComponentModel;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Mvc;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class GetTopBooksEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("TopBook", async (
            ElasticsearchClient client,
            CancellationToken token,
            [FromQuery] string[]? categories,
            [FromQuery] [Description("must be between 0 and 100")]
            int limit = 10) =>
        {
            if (limit is <= 0 or > 100)
                return Results.Problem("Limit must be between 0 and 100", statusCode: StatusCodes.Status400BadRequest);

            var categoryKeywordField = Infer.Field<BookEntityModel>(b => b.Categories.Suffix("keyword"));

            var query = new List<Query>();
            if (categories?.Length > 0)
                query.Add(new TermsQuery(categoryKeywordField, categories.Select(FieldValue.String).ToArray()));

            var booksResponse = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Size(limit)
                    .Query(q => q
                        .Bool(b => b
                            .Must(query)
                        )
                    )
                    .Sort(s => s
                        .Field(f => f.Rating, SortOrder.Desc)
                    )
                    .Source(s => s
                        .Filter(f => f
                            .Includes(
                                i => i.ISBN,
                                i => i.Title,
                                i => i.Author,
                                i => i.Publisher,
                                i => i.Categories,
                                i => i.Rating)))
                , token
            );

            if (!booksResponse.IsValidResponse)
                return Results.Problem("Search Failed.", statusCode: StatusCodes.Status500InternalServerError);

            return Results.Ok(new GetTopBooksResponse(
                booksResponse.Documents.Count,
                booksResponse.Documents.Select(b => new GetTopBookResponse(
                    b.ISBN,
                    b.Title,
                    b.Author,
                    b.Publisher,
                    b.Categories,
                    b.Rating)).ToList())
            );
        });
    }

    private record GetTopBooksResponse(
        long TotalBooks,
        List<GetTopBookResponse> Books
    );

    private record GetTopBookResponse(
        string Isbn,
        string Title,
        string Author,
        string Publisher,
        string[] Categories,
        double Rating);
}