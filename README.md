# AzDo Boards Project Management Tool

AzDo Boards ("As-Do") is to be a tool to assist with project management within Azure DevOps Boards. The UI within Azure DevOps Boards is clunky for doing project management and quickly adding or updating information. 

The intent is to support a custom-defined hierarchy (e.g., Initiative > Epic > Feature > User Story / Bug / Research > Tasks).

*This is an idea that has been noodled for a while. Now this is being attempted for future efficiency of project work, to give back, and to see if there is interest within the open source community. We will see where this goes, if anywhere.*

## Future Action Items

1. Add support for certificates in place of Client Secret for the App Registration and Azure DevOps downstream API support.

# Development Technologies

- Visual Studio 2022 17.14.13 Preview 1.0+
- .NET 10 Preview
- Microsoft TFS Client (to abstract Azure DevOps API calls)
- Redis Cache - one of the following:
  - [Memurai](https://www.memurai.com/get-memurai) (Free version of Redis Cache on Developer Machine - can also purchase Enterprise Edition)
  - Azure Cache for Redis (Microsoft-based free tier for Development, higher tiers for non-Development environments)
  - Redis for Windows ([tporadowski/redis](https://github.com/tporadowski/redis/releases)) - last update February 17, 2022


# Configuration

## Application

Generalized settings are in [appsettings.json](./appsettings.json) and checked into source control. For development-based work, secrets.json can be leveraged for sensitive information, which isn't used for non-development environments. 

Azure KeyVault can be leveraged for all environments by setting the environmental variable **KeyVaultEndpoint** (e.g., in the Azure App Service or in a local Windows machine), making sure the hosted application has access to the Key Vault with GET and LIST permissions. Visual Studio can be configured for such access within the IDE directly and the user directly assigned. App Services can have their security principle assigned to the KeyVault for direct access up application startup.

The settings supported are as follows:

```
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/<ORGANIZATION_NAME>"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "CallbackPath": "/signin-oidc"
    "Domain": "<DOMAIN>.onmicrosoft.com",
    "TenantId": "<TENANT_ID>",
    "ClientId": "<CLIENT_ID>",
    "ClientSecret": "<CLIENT SECRET>"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "AzDoBoardsTokenCache"
  }
}
```

## Azure Portal App Registration

An App Registration is required to allow this application to integrate with Azure DevOps using Azure Entra ID authentication (presuming Azure DevOps is integrated into Azure Entra ID via Organization Settings > Microsoft Entra).

1. View the Overview option and capture the *Application (client) ID* for the **ClientId** and the *Directory (tenant) ID* for the **TenantId**.

2. Note your *Domain*, which is the the *Publisher domain* (Manage > Branding & properties) for the **Domain**.

3. Create a *Client secret* (Overview > Client credentials ***OR*** Manage > Certificates & secrets), for the **ClientSecret**, noting the Value (this is the Client Secret and can only be seen and captured during creation). This is used for Azure DevOps Downstream API support so it can obtain an access token for the API calls leveraged by the Microsoft MFS Client library. Note that this has an expiration date of up to 2 years.

4. Configure a web application authentication (Manage > Authentication)

   - Redirect URLs (Redirect URI configuration tab within Authentication option ***OR*** directly available via Overview > Redirect URIs) with a redirect URI (e.g., https://localhost:7035/signin-oidc for local running within Visual Studio). More than one URL is supported for different environments.
   - Settings (Settings tab), make sure ID tokens is enabled (checked), for the hybrid flow (Blazor Server doesn't use implicit flow since it isn't a SPA and doesn't require additional configuration).

5. (Optional) Additional claims (Manage > Token configuration) by adding optional claims for the *ID Token* (Access Token not used as this is for a SPA web app), such as email, family_name, given_name, ipaddr, preferred_username.

6. The following API Permissions (Manage > API permissions) are required:

   - Azure DevOps: user_impersonation
   - Microsoft Graph: User.Read

   Additional are automatically added based on the additional claims desired. For the examples given, email and profile are required for the claims to be received.

   To have the API permissions configured for all users up front so they don't have to approve during login, use the **Enterprise applications** blade (link to it is available via Manage > API permissions) to view and manage consented permissions for individual apps as well as the tenant's consent settings.

   



\~End\~