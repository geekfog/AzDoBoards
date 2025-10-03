namespace AzDoBoards.Utility;

public static class Constants
{
    public const string App_Name_Full = "AzDo Boards";
    public const string App_Name_Short = "AzDoBoards";

    public const string Azure_ConfigSection = "AzureAd";
    public const string Azure_EnvcfgKeyVaultEndpoint = "KeyVaultEndpoint";
    public const string Azure_ConfigStorageConnectionString = "Azure:StorageAccountConnectionString";
    public const string Azure_DefaultStorageConnectionString = "UseDevelopmentStorage=true"; // Default to Azure Storage Emulator (Azurite)
    public const string Azure_StorageTableName = App_Name_Short;
    public const string Azure_StorageTableSetting = $"{App_Name_Short}Setting"; // No special characters allowed
    public const string Azure_StorageTableLog = $"{App_Name_Short}Serilog"; // No special characters allowed

    public const string Serilog_TemplateNoDate = "{Level:u3}: {Message:lj}{NewLine}";
    public const string Serilog_Template = "{Timestamp:yyyy-MM-dd HH:mm:ss} " + Serilog_TemplateNoDate;
    public const string Serilog_LogFilePath = "Logs/serilog-.txt"; // Path for Serilog log files
    public const int    Serilog_RollingIntervalDays = 7; // Number of days to keep log files

    public const string AzureDevOps_OAuthScope = "499b84ac-1321-427f-aa17-267ca6975798/.default"; // Azure DevOps OAuth scope used for API authentication (Azure DevOps Resource ID and grant access to all permissions the app has been configured) 
    public const string AzureDevOps_ConfigOrganizationUrl = "AzureDevOps:OrganizationUrl";

    public const string Redis_DefaultConnectionString = "localhost:6379";
    public const string Redis_TokenCacheInstanceName = $"{App_Name_Short}TokenCache";
    public const string Redis_ConfigConnectionString = "Redis:ConnectionString";

    public const string Page_FoundFoundPath = "/not-found";

    public const int    Security_HstsMaxAgeDays = 365;
}
