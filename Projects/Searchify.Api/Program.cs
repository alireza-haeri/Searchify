using System.Reflection;
using Carter;
using FluentValidation;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Searchify.Api.Common;
using Searchify.Api.ElasticSearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApplicationSetting>(builder.Configuration);
var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<ApplicationSetting>>().Value;

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder
    .AddElasticSearch(appSettings)
    .AddElasticSearchConfigurations();

builder.Services.AddOpenApi();
builder.Services.AddCarter();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

await app.UseElasticSearchAsync();

app.MapCarter();

app.Run();