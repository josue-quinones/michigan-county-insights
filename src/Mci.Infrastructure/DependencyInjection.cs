using Mci.Core.Application.Reporting;
using Mci.Infrastructure.Census;
using Mci.Infrastructure.Importing;
using Mci.Infrastructure.Persistence;
using Mci.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mci.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MciDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'MciDatabase' is required. Set ConnectionStrings:MciDatabase or ConnectionStrings__MciDatabase.");
        }

        services.AddDbContext<MciDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<CensusOptions>(configuration.GetSection(CensusOptions.SectionName));
        services.Configure<ImportOptions>(configuration.GetSection(ImportOptions.SectionName));

        services.AddHttpClient<CensusAcsClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CensusOptions>>().Value;
            client.BaseAddress = new Uri(NormalizeBaseUrl(options.BaseUrl), UriKind.Absolute);
        });

        services.AddScoped<RawAcsStagingImportService>();
        services.AddScoped<CountyMetricFactLoadService>();
        services.AddScoped<IReportingQueryService, ReportingQueryService>();

        return services;
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Configuration value 'Census:BaseUrl' is required.");
        }

        return baseUrl.EndsWith("/", StringComparison.Ordinal)
            ? baseUrl
            : $"{baseUrl}/";
    }
}
