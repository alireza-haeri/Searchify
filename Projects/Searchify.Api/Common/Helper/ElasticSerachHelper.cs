using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Searchify.Api.Common.Helper;

public static class ElasticSearchHelper
{
    public static void AddMatchIfNotEmpty(this List<Query> queries, Field field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            queries.Add(new MatchQuery { Field = field, Query = value });
    }

    public static void AddTermIfNotEmpty(this List<Query> queries, Field field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            queries.Add(new TermQuery { Field = field, Value = value });
    }
}