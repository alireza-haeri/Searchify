using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book;

public class CreateBookEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapPost("", async (CreateBookRequest request, ElasticsearchClient client,CancellationToken token) =>
        {
            var validationResult = await new CreateBookRequestValidator().ValidateAsync(request, token);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var exist = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Query(q => q
                        .Term(m => m
                            .Field(f => f.ISBN)
                            .Value(request.Isbn)
                        )
                    )
                , token);
            
            var hit = exist.Hits.FirstOrDefault();
            if (hit is not null)
                return Results.Problem($"Book with ISBN: {request.Isbn} already exists",statusCode:StatusCodes.Status409Conflict);
            
            var index = request.ToEntity();

            var response = await client.IndexAsync(index, d => d.Index(BookEntityModel.IndexName), token);
            if (!response.IsValidResponse)
                return Results.BadRequest();
                
            return Results.Created($"/api/book/{index.ISBN}", 
                new CreateBookResponse(
                index.Title,
                index.Author,
                index.Publisher,
                index.ISBN,
                index.Description,
                index.Categories,
                index.PublishDate,
                index.PageCount,
                index.Rating));
        })
        .WithSummary("CreateBook");
    }

    public record CreateBookRequest(
        string Title,
        string Author,
        string Publisher,
        string Isbn,
        string Description,
        string[] Categories,
        DateTime PublishDate,
        int PageCount,
        double Rating
    )
    {
        public BookEntityModel ToEntity()
            => BookEntityModel.Create(Title, Author, Publisher, Isbn, Description, Categories, PublishDate, PageCount,
                Rating);
    }

    public record CreateBookResponse(
        string Title,
        string Author,
        string Publisher,
        string Isbn,
        string Description,
     string[] Categories,
        DateTime PublishDate,
        int PageCount,
        double Rating
    );

    public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
    {
        public CreateBookRequestValidator()
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
                .Must(c => c.Length > 0).WithMessage("At least one category is required.");

            RuleFor(x => x.PublishDate)
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Publish date cannot be in the future.");

            RuleFor(x => x.PageCount)
                .GreaterThan(0).WithMessage("Page count must be greater than zero.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");
        }
    }
}