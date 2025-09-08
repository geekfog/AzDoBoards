namespace AzDoBoards.Utility;

public static class Server
{
    public static bool IsDevelopment
    {
        get
        {
            var environment = AspNetCoreEnvironment;
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(environment, "DEV", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(environment, "Debug", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static bool IsProduction
    {
        get
        {
            var environment = AspNetCoreEnvironment;
            return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(environment, "PRD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(environment, "Release", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string AspNetCoreEnvironment => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
}