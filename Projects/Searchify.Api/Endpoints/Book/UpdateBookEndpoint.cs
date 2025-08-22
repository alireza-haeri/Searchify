using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book;

public class UpdateBookEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapPut("{isbn}", async (UpdateBookRequest request, string isbn, ElasticsearchClient client,CancellationToken token) =>
        {
            var validationResult = await new UpdateBookRequestValidator().ValidateAsync(request, token);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var book = await client.SearchAsync<BookEntityModel>(b => b
                .Size(1)
                .Query(q => q
                    .Term(m => m
                        .Field(f => f
                            .ISBN)
                        .Value(isbn))), token);
            var hit = book.Hits.FirstOrDefault();
            var data = book.Documents.FirstOrDefault();

            if (hit is null || data is null)
                return Results.NotFound("Book not found");

            if (request.Isbn != data.ISBN)
            {
                var exist = await client.SearchAsync<BookEntityModel>(a => a
                        .Indices(BookEntityModel.IndexName)
                        .Size(1)
                        .Query(q => q
                            .Term(m => m
                                .Field(f => f.ISBN)
                                .Value(request.Isbn)
                            )
                        )
                    , token);
            
                var existHit = exist.Hits.FirstOrDefault();
                if (existHit is not null)
                    return Results.Problem($"Book with ISBN: {request.Isbn} already exists",statusCode:StatusCodes.Status409Conflict);
            }
            
            var response = await client.UpdateAsync<BookEntityModel, object>(
                index: BookEntityModel.IndexName,
                id: hit.Id,
                descriptor => descriptor
                    .Doc(request), cancellationToken: token);
            if (!response.IsValidResponse)
                return Results.BadRequest();

            return Results.Ok(new UpdateBookResponse(
                request.Title,
                request.Author,
                request.Publisher,
                request.Isbn,
                request.Description,
                request.Categories,
                request.PublishDate,
                request.PageCount,
                request.Rating));
        })
        .WithSummary("UpdateBook");
    }

    public record UpdateBookRequest(
        string Title,
        string Author,
        string Publisher,
        string Isbn,
        string Description,
        List<string> Categories,
        DateTime PublishDate,
        int PageCount,
        double Rating
    );

    public record UpdateBookResponse(
        string Title,
        string Author,
        string Publisher,
        string Isbn,
        string Description,
        List<string> Categories,
        DateTime PublishDate,
        int PageCount,
        double Rating
    );

    public sealed class UpdateBookRequestValidator : AbstractValidator<UpdateBookRequest>
    {
        public UpdateBookRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Author)
                .NotEmpty().WithMessage("Author is required.")
                .MaximumLength(100).WithMessage("Author name must not exceed 100 characters.");

            RuleFor(x => x.Publisher)
                .NotEmpty().WithMessage("Publisher is required.")
                .MaximumLength(100).WithMessage("Publisher name must not exceed 100 characters.");

            RuleFor(x => x.Isbn)
                .NotEmpty().WithMessage("ISBN is required.")
                .Length(10, 13).WithMessage("ISBN must be between 10 and 13 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.Categories)
                .NotNull().WithMessage("Categories must be provided.")
                .Must(c => c.Count > 0).WithMessage("At least one category is required.");

            RuleFor(x => x.PublishDate)
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Publish date cannot be in the future.");

            RuleFor(x => x.PageCount)
                .GreaterThan(0).WithMessage("Page count must be greater than zero.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");
        }
    }
}