namespace Searchify.Api.Common;

public class ApplicationSetting
{
    public required ElasticSearchConfiguration ElasticSearchConfiguration { get; set; }
}

public class ElasticSearchConfiguration
{
    public required string Url { get; init; }
}