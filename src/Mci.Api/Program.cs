using Mci.Api.GraphQL;
using Mci.Api.Operations;
using Mci.Api.Reporting;
using Mci.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var allowedCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache(options =>
{
    // Reporting data only changes on import, so a short shared cache is a cheap win.
    // Vary by the query keys used across reporting + county-insight endpoints; the
    // route {fips} is part of the path and varies automatically.
    options.AddPolicy("Reporting", policy => policy
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery(
            "metricCode",
            "countyFipsCode",
            "releaseYear",
            "left",
            "right",
            "release"));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
    {
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ReportingGraphQlQueries>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (allowedCorsOrigins.Length > 0)
{
    app.UseCors("Dashboard");
}

app.UseOutputCache();

app.MapHealthChecks("/health");
app.MapOperationsEndpoints();
app.MapReportingEndpoints();
app.MapCountyInsightsEndpoints();
app.MapGraphQL("/graphql");
app.MapGet("/", () => Results.Redirect("/health"));

app.Run();
