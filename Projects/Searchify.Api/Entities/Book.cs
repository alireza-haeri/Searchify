using Elastic.Clients.Elasticsearch;
using Searchify.Api.ElasticSearch.Interfaces;

namespace Searchify.Api.Entities;

public class BookEntityModel
{
    public const string IndexName = "book";

    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Publisher { get; set; }
    public string ISBN { get; set; }
    public string Description { get; set; }
    public List<string> Categories { get; set; }
    public DateTime PublishDate { get; set; }
    public int PageCount { get; set; }
    public double Rating { get; set; }
}

public class BookEntityConfiguration : IElasticSearchConfigurationBuilder
{
    private const string CustomAnalyzer = "custom_text_analyzer";

    public async Task ConfigureAsync(ElasticsearchClient client)
    {
        var indexExist = await client.ExistsAsync(BookEntityModel.IndexName);
        if (indexExist.Exists)
            return;

        var response = await client.Indices.CreateAsync(BookEntityModel.IndexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(0)
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom(CustomAnalyzer, ca => ca
                            .Tokenizer("standard")
                            .Filter("lowercase", "asciifolding")
                        )
                    )
                )
            )
            .Mappings<BookEntityModel>(m => m
                .Properties(p => p
                    .Text(t => t.Title, d => d
                        .Analyzer(CustomAnalyzer)
                        .Boost(3)
                    )
                    .Text(t => t.Description, d => d
                        .Analyzer(CustomAnalyzer)
                    )
                    .Text(t => t.Author, d => d
                        .Analyzer(CustomAnalyzer)
                        .Fields(f => f
                            .Keyword("keyword")
                        )
                    )
                    .Text(t => t.Publisher, d => d
                        .Analyzer(CustomAnalyzer)
                        .Fields(f => f
                            .Keyword("keyword")
                        )
                    )
                    .Keyword(k => k.ISBN)
                    .Text(t => t.Categories, d => d
                        .Analyzer(CustomAnalyzer)
                        .Fields(f => f
                            .Keyword("keyword")
                        )
                    )
                    .Date(d => d.PublishDate)
                    .IntegerNumber(n => n.PageCount)
                    .DoubleNumber(dn => dn.Rating)
                )
            )
        );
    }
}
