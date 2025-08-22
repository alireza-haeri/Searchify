using Elastic.Clients.Elasticsearch;

namespace Searchify.Api.ElasticSearch.Interfaces;

public interface IElasticSearchConfigurationBuilder
{
    Task ConfigureAsync(ElasticsearchClient client);
}