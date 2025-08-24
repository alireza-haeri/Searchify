using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class GetBookByIsbnEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("{isbn:length(10,13)}", async (
            ElasticsearchClient client,
            CancellationToken token,
            [FromRoute] string? isbn) =>
        {
            if (isbn is null  || isbn.Length > 13 || isbn.Length < 10)
                return Results.Problem("ISBN must be between 10 and 13 characters.",
                    statusCode: StatusCodes.Status400BadRequest);

            var isbnKeywordField = Infer.Field<BookEntityModel>(x => x.ISBN.Suffix("keyword"));
            var bookResponse = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Query(q => q
                        .Term(t => t
                            .Field(isbnKeywordField)
                            .Value(isbn)
                        )
                    )
                    .Source(s => s
                        .Filter(f => f
                            .Includes(
                                i => i.Title,
                                i => i.Author,
                                i => i.Publisher,
                                i => i.Description,
                                i => i.Categories,
                                i => i.PublishDate,
                                i => i.PageCount,
                                i => i.Rating)))
                , token);

            if (!bookResponse.IsValidResponse)
                return Results.Problem("Search Failed", statusCode: StatusCodes.Status500InternalServerError);

            var hit = bookResponse.Hits.FirstOrDefault();
            var data = bookResponse.Documents.FirstOrDefault();
            if (data is null || hit is null)
                return Results.NotFound($"Book With Isbn:{isbn} Not Found.");

            return Results.Ok(new GetBookByIsbnResponse(
                hit.Id,
                data.Title,
                data.Author,
                data.Publisher,
                data.Description,
                data.Categories,
                data.PublishDate,
                data.PageCount,
                data.Rating));
        });
    }

    private record GetBookByIsbnResponse(
        string Id,
        string Title,
        string Author,
        string Publisher,
        string Description,
        string[] Categories,
        DateTime PublishDate,
        int PageCount,
        double Rating);
}