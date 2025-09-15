namespace AzDoBoards.Utility;

public static class Constants
{
    public const string Azure_ConfigSection = "AzureAd";
    public const string Azure_EnvcfgKeyVaultEndpoint = "KeyVaultEndpoint";

    public const string AzureDevOps_OAuthScope = "499b84ac-1321-427f-aa17-267ca6975798/.default"; // Azure DevOps OAuth scope used for API authentication (Azure DevOps Resource ID and grant access to all permissions the app has been configured) 
    public const string AzureDevOps_ConfigOrganizationUrl = "AzureDevOps:OrganizationUrl";

    public const string Redis_DefaultConnectionString = "localhost:6379";
    public const string Redis_TokenCacheInstanceName = "AzDoBoardsTokenCache";
    public const string Redis_ConfigConnectionString = "Redis:ConnectionString";

    public const string Page_FoundFoundPath = "/not-found";

    public const int Security_HstsMaxAgeDays = 365;
}
