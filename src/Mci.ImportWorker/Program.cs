using Mci.Core.Domain.Enums;
using Mci.Infrastructure;
using Mci.Infrastructure.Analytics;
using Mci.Infrastructure.Importing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var command = args.FirstOrDefault() ?? "raw-stage";

if (string.Equals(command, "raw-stage", StringComparison.OrdinalIgnoreCase))
{
    await RunRawStageAsync(scope.ServiceProvider);
}
else if (string.Equals(command, "load-facts", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(command, "load-direct-facts", StringComparison.OrdinalIgnoreCase))
{
    await RunFactLoadAsync(scope.ServiceProvider);
}
else if (string.Equals(command, "stage-and-load-facts", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(command, "stage-and-load-direct-facts", StringComparison.OrdinalIgnoreCase))
{
    await RunRawStageAsync(scope.ServiceProvider);
    await RunFactLoadAsync(scope.ServiceProvider);
}
else if (string.Equals(command, "clarity-insights", StringComparison.OrdinalIgnoreCase))
{
    await RunClarityInsightsAsync(scope.ServiceProvider);
}
else
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Supported commands: raw-stage, load-facts, stage-and-load-facts, clarity-insights");
    return 1;
}

return 0;

static async Task RunRawStageAsync(IServiceProvider serviceProvider)
{
    var importService = serviceProvider.GetRequiredService<RawAcsStagingImportService>();
    var result = await importService.ImportDefaultAcsReleaseAsync(ImportTriggerType.Manual);

    Console.WriteLine($"Import run: {result.ImportRunId}");
    Console.WriteLine($"Data release: {result.DataReleaseId}");
    Console.WriteLine($"County rows fetched: {result.RecordsFetched}");
    Console.WriteLine($"Raw staging rows written: {result.RecordsStaged}");
    Console.WriteLine($"Validation issues: {result.IssueCount}");
    Console.WriteLine($"Validation errors: {result.ErrorCount}");
    Console.WriteLine($"Validation warnings: {result.WarningCount}");
}

static async Task RunClarityInsightsAsync(IServiceProvider serviceProvider)
{
    var clarityService = serviceProvider.GetRequiredService<ClarityInsightsImportService>();
    var result = await clarityService.FetchAndStoreDailyInsightsAsync();

    Console.WriteLine($"Capture date: {result.CaptureDate:yyyy-MM-dd}");
    Console.WriteLine($"Executed: {result.Executed}");
    Console.WriteLine($"Already captured today: {result.AlreadyCaptured}");

    if (result.SkipReason is not null)
    {
        Console.WriteLine($"Skip reason: {result.SkipReason}");
    }

    Console.WriteLine($"Snapshot id: {result.SnapshotId}");
    Console.WriteLine($"Total sessions: {result.TotalSessionCount}");
    Console.WriteLine($"Distinct users: {result.DistinctUserCount}");
}

static async Task RunFactLoadAsync(IServiceProvider serviceProvider)
{
    var factLoadService = serviceProvider.GetRequiredService<CountyMetricFactLoadService>();
    var result = await factLoadService.LoadMetricsFromLatestValidatedStagingAsync();

    Console.WriteLine($"Import run: {result.ImportRunId}");
    Console.WriteLine($"Data release: {result.DataReleaseId}");
    Console.WriteLine($"Fact rows inserted: {result.RecordsInserted}");
    Console.WriteLine($"Transform/load issues: {result.IssueCount}");
    Console.WriteLine($"Transform/load errors: {result.ErrorCount}");
    Console.WriteLine($"Transform/load warnings: {result.WarningCount}");
    Console.WriteLine($"Already loaded: {result.AlreadyLoaded}");
}
