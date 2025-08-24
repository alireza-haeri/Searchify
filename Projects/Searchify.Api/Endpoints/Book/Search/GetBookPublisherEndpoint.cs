using Elastic.Clients.Elasticsearch;
using Searchify.Api.Endpoints.Common;
using Searchify.Api.Entities;

namespace Searchify.Api.Endpoints.Book.Search;

public class GetBookPublisherEndpoint : BookEndpointBase
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = MapBookGroup(app);

        group.MapGet("publishers", async (
            ElasticsearchClient client,
            CancellationToken token) =>
        {
            var publisherKeywordField = Infer.Field<BookEntityModel>(b => b.Publisher.Suffix("keyword"));

            var publishersResponse = await client.SearchAsync<BookEntityModel>(a => a
                    .Indices(BookEntityModel.IndexName)
                    .Size(0)
                    .Aggregations(aggregation => aggregation
                        .Add("publishers", ag => ag
                            .Terms(t => t
                                .Field(publisherKeywordField)
                                .Size(100)
                                .Order(order => order
                                    .Add("avg_rating", SortOrder.Desc)
                                )
                            )
                            .Aggregations(sa => sa
                                .Add("avg_rating", subAggregation => subAggregation
                                    .Avg(avg => avg
                                        .Field(f => f.Rating)
                                    )
                                )
                            )
                        )
                        .Add("total_publishers", ag => ag
                            .Cardinality(c => c
                                .Field(publisherKeywordField)
                            )
                        )
                    ), token
            );

            if (!publishersResponse.IsValidResponse)
                return Results.Problem("Search Failed.", statusCode: StatusCodes.Status500InternalServerError);

            var totalPublishers = publishersResponse.Aggregations?
                .GetCardinality("total_publishers")?.Value ?? 0;
            var bucket = publishersResponse.Aggregations?
                .GetStringTerms("publishers")?
                .Buckets
                .Select(b => new GetBookPublisherResponse(
                        b.Key.ToString(),
                        b.Aggregations?.GetAverage("avg_rating")?.Value ?? 0
                    )
                )
                .ToList()
                ?? [];
            
            return Results.Ok(new GetBookPublishersResponse(
                totalPublishers,
                bucket));
        });
    }

    private record GetBookPublishersResponse(
        long TotalPublishers,
        List<GetBookPublisherResponse> Publishers);
    private record GetBookPublisherResponse(string Publisher,double BooksRating);
}