using Microsoft.Extensions.Hosting;
using Serilog;

namespace AzDoBoards.Utility;

public static class SerilogHelper
{
    public static void Configure(IHostEnvironment environment)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(environment.IsDevelopment() ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
            .WriteTo.Console();

        if (environment.IsDevelopment()) loggerConfig.WriteTo.File($"logs\\{nameof(AzDoBoards)}.log", rollingInterval: RollingInterval.Day);

        Log.Logger = loggerConfig.CreateLogger();
        Header();
    }

    private static void Header()
    {
        Log.Information("                                                                                  ");
        Log.Information("░█████╗░███████╗██████╗░░█████╗░░░██████╗░░█████╗░░█████╗░██████╗░██████╗░░██████╗");
        Log.Information("██╔══██╗╚════██║██╔══██╗██╔══██╗░░██╔══██╗██╔══██╗██╔══██╗██╔══██╗██╔══██╗██╔════╝");
        Log.Information("███████║░░███╔═╝██║░░██║██║░░██║░░██████╦╝██║░░██║███████║██████╔╝██║░░██║╚█████╗░");
        Log.Information("██╔══██║██╔══╝░░██║░░██║██║░░██║░░██╔══██╗██║░░██║██╔══██║██╔══██╗██║░░██║░╚═══██╗");
        Log.Information("██║░░██║███████╗██████╔╝╚█████╔╝░░██████╦╝╚█████╔╝██║░░██║██║░░██║██████╔╝██████╔╝");
        Log.Information("╚═╝░░╚═╝╚══════╝╚═════╝░░╚════╝░░░╚═════╝░░╚════╝░╚═╝░░╚═╝╚═╝░░╚═╝╚═════╝░╚═════╝░");
        Log.Information("                                                                                  ");
        // TODO: Parameterize the time zone - use the Windows time zone also specified for Windows in Pipelines (same parameter)?
        Log.Information($"Application started on {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")):yyyy-MM-dd HH:mm:ss} CST, {DateTime.Now:yyyy-MM-dd HH:mm:ss} NOW");
        Log.Information($"Current working folder: {Directory.GetCurrentDirectory()}");
        Log.Information("                                                                                               ");
    }
}
