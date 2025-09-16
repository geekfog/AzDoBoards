using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AzDoBoards.Utility;

public static class SerilogHelper
{
    public static void Configure(IHostEnvironment environment, IConfiguration configuration)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(environment.IsDevelopment() ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
            .WriteTo.Console(outputTemplate: Constants.Serilog_Template)
            .Enrich.WithProperty("AspNetCoreEnvironment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown")
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Region", Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown");

        var logStorageConnectionString = configuration[Constants.Azure_ConfigStorageConnectionString] ?? (environment.IsDevelopment() ? Constants.Azure_DefaultStorageConnectionString : string.Empty);
        if (!string.IsNullOrEmpty(logStorageConnectionString)) 
            loggerConfig.WriteTo.AzureTableStorage(connectionString: logStorageConnectionString, storageTableName: Constants.Azure_StorageTableLog, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);

        if (environment.IsDevelopment()) 
            loggerConfig.WriteTo.File($"{Constants.Serilog_LogFilePath}", rollingInterval: RollingInterval.Day);

        Log.Logger = loggerConfig.CreateLogger();
        Header();
    }

    private static void Header()
    {
        Log.Information("                                                                                  ");
        Log.Information("░█████╗░███████╗██████╗░░█████╗░░░░██████╗░░█████╗░░█████╗░██████╗░██████╗░░██████╗");
        Log.Information("██╔══██╗╚════██║██╔══██╗██╔══██╗░░░██╔══██╗██╔══██╗██╔══██╗██╔══██╗██╔══██╗██╔════╝");
        Log.Information("███████║░░███╔═╝██║░░██║██║░░██║░░░██████╦╝██║░░██║███████║██████╔╝██║░░██║╚█████╗░");
        Log.Information("██╔══██║██╔══╝░░██║░░██║██║░░██║░░░██╔══██╗██║░░██║██╔══██║██╔══██╗██║░░██║░╚═══██╗");
        Log.Information("██║░░██║███████╗██████╔╝╚█████╔╝░░░██████╦╝╚█████╔╝██║░░██║██║░░██║██████╔╝██████╔╝");
        Log.Information("╚═╝░░╚═╝╚══════╝╚═════╝░░╚════╝░░░░╚═════╝░░╚════╝░╚═╝░░╚═╝╚═╝░░╚═╝╚═════╝░╚═════╝░");
        Log.Information("                                                                                  ");
        // TODO: Parameterize the time zone - use the Windows time zone also specified for Windows in Pipelines (same parameter)?
        Log.Information($"Application started on {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")):yyyy-MM-dd HH:mm:ss} CST, {DateTime.Now:yyyy-MM-dd HH:mm:ss} NOW");
        Log.Information($"Current working folder: {Directory.GetCurrentDirectory()}");
        Log.Information("                                                                                               ");
    }
}
