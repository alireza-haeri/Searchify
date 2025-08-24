using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class GetBookCategoriesEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("categories", async (
            ElasticsearchClient client,
            CancellationToken token) =>
        {
            var categoriesKeywordField = Infer.Field<BookEntityModel>(b => b.Categories.Suffix("keyword"));

            var categoriesResponse = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Size(0)
                    .Aggregations(ag => ag
                        .Add("categories", aggregation => aggregation
                            .Terms(t => t
                                .Field(categoriesKeywordField)
                                .Size(100)
                                .Order(o => o
                                    .Add("avg_rating", SortOrder.Desc))
                            )
                            .Aggregations(sub => sub
                                .Add("avg_rating", subAggregation => subAggregation
                                    .Avg(avg => avg
                                        .Field(f => f.Rating)
                                    )
                                )
                            )
                        )
                        .Add("total_categories", aggregation => aggregation
                            .Cardinality(c => c
                                .Field(categoriesKeywordField)
                            )
                        )
                    ), token
            );

            if (!categoriesResponse.IsValidResponse)
                return Results.Problem("Search Failed", statusCode: StatusCodes.Status500InternalServerError);

            var totalCategories = categoriesResponse.Aggregations?
                .GetCardinality("total_categories")?.Value ?? 0;
            var bucket = categoriesResponse.Aggregations!
                .GetStringTerms("categories")?
                .Buckets
                .Select(b =>
                    new GetBookCategoryResponse(
                        b.Key.ToString(),
                        b.Aggregations?.GetAverage("avg_rating")?.Value ?? 0)
                )
                .ToList()
                ?? [];

            return Results.Ok(new GetBookCategoriesResponse(
                totalCategories,
                bucket));
        });
    }

    private record GetBookCategoriesResponse(
        long TotalCount,
        List<GetBookCategoryResponse> Categories
    );

    private record GetBookCategoryResponse(string Title, double AvgRating);
}