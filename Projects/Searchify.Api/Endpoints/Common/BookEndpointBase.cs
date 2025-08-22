using Carter;

namespace Searchify.Api.Endpoints.Common;

public abstract class BookEndpointBase : ICarterModule
{
    public abstract void AddRoutes(IEndpointRouteBuilder app);

    protected static RouteGroupBuilder MapBookGroup(IEndpointRouteBuilder app)
        => app.MapGroup("/api/books")
            .WithTags("Book");
}