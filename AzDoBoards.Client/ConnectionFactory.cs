using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using AzDoBoards.Utility;

namespace AzDoBoards.Client;

// ConnectionFactory is responsible for creating VssConnection instances
public class ConnectionFactory
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private VssConnection? _connection;

    private readonly string _organizationUrl;

    // Constructor injects token acquisition, HTTP context accessor, and organization URL
    public ConnectionFactory(ITokenAcquisition tokenAcquisition, IHttpContextAccessor httpContextAccessor, string organizationUrl)
    {
        _tokenAcquisition = tokenAcquisition;
        _httpContextAccessor = httpContextAccessor;
        _organizationUrl = organizationUrl;
    }

    // Returns a VssConnection for the current authenticated user
    public async Task<VssConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            await InitializeConnectionAsync();
        }
        return _connection!;
    }

    // Initializes the VssConnection using the current user's access token
    private async Task InitializeConnectionAsync()
    {
        // Get the current authenticated user from the HTTP context
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
            throw new InvalidOperationException("User is not authenticated.");

        // Acquire the access token for Azure DevOps using the current user
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { Utility.Constants.AzureDevOps_OAuthScope }, user: user);
            var creds = new VssOAuthAccessTokenCredential(token);
            _connection = new VssConnection(new Uri(_organizationUrl), creds);
        }
        catch
        {
            throw;
        }
    }
}