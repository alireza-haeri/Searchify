using Elastic.Clients.Elasticsearch;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book;

public class DeleteBookEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapDelete("{isbn}", async (string isbn, ElasticsearchClient client, CancellationToken token) =>
        {
            var book = await client.SearchAsync<BookEntityModel>(a => a
                .Indices(BookEntityModel.IndexName)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.ISBN)
                        .Query(isbn)
                    )
                ), token);

            var hit = book.Hits.FirstOrDefault();
            if (hit is null)
                return Results.NotFound("Book not found");

            var result = await client.DeleteAsync<BookEntityModel>(
                index: BookEntityModel.IndexName,
                id: hit.Id,
                d => d.Index(BookEntityModel.IndexName), cancellationToken: token);

            if (!result.IsValidResponse)
                return Results.BadRequest();
            
            return Results.Ok();
        });
    }
}