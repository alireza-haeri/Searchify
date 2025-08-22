using Elastic.Clients.Elasticsearch;
using Searchify.Api.Common;
using Searchify.Api.ElasticSearch.Interfaces;

namespace Searchify.Api.ElasticSearch;

public static class ElasticSearchConfigurationExtensions
{
    public static WebApplicationBuilder AddElasticSearch(this WebApplicationBuilder builder, ApplicationSetting options)
    {
        var setting = new ElasticsearchClientSettings(new Uri(options.ElasticSearchConfiguration.Url))
            .PingTimeout(TimeSpan.FromSeconds(10));
        var elasticSearchClient = new ElasticsearchClient(setting);

        builder.Services.AddSingleton(elasticSearchClient);
        return builder;
    }

    public static WebApplicationBuilder AddElasticSearchConfigurations(this WebApplicationBuilder builder)
    {
        var configurationTypeServices =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(IElasticSearchConfigurationBuilder).IsAssignableFrom(t)
                    && t is { IsInterface: false, IsClass: true, IsAbstract: false })
                .Select(c =>
                    new ServiceDescriptor(typeof(IElasticSearchConfigurationBuilder), c, ServiceLifetime.Singleton));

        foreach (var configurationTypeService in configurationTypeServices)
            builder.Services.Add(configurationTypeService);
        
        return builder;
    }

    public static async Task UseElasticSearchAsync(this WebApplication app)
    {
        var elasticSearch = app.Services.GetRequiredService<ElasticsearchClient>();
        var configurations = app.Services.GetServices<IElasticSearchConfigurationBuilder>();

        foreach (var configuration in configurations)
            await configuration.ConfigureAsync(elasticSearch);
    }
}