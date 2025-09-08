using AzDoBoards.Client;
using AzDoBoards.Ui.Components;
using AzDoBoards.Utility;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using MudBlazor.Services;
using Serilog;
using StackExchange.Redis;

namespace AzDoBoards.Ui;

public class Program
{
    public static void Main(string[] args)
    {
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Build Services
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        var builder = WebApplication.CreateBuilder(args);

        // Add support for secrets.json for development
        if (Server.IsDevelopment) builder.Configuration.AddUserSecrets(nameof(AzDoBoards));

        // Add Serilog Support
        builder.Services.AddSerilog();
        SerilogHelper.Configure(builder.Environment);

        // Add Azure KeyVault configuration
        var keyVaultEndpoint = Environment.GetEnvironmentVariable("KeyVaultEndpoint");
        if (!string.IsNullOrEmpty(keyVaultEndpoint))
        {
            var keyVaultEndpointUrl = new Uri(keyVaultEndpoint);
            try
            {
                builder.Configuration.AddAzureKeyVault(keyVaultEndpointUrl, new DefaultAzureCredential());
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding KeyVault configuration: {ex.Message}");
            }
        }

        if (Server.IsProduction)
        {
            builder.Services.AddHsts(options => // https://aka.ms/aspnetcore-hsts.
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }

        // Add Mudblazor
        builder.Services.AddMudServices();

        // Add Redis Cache
        var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString; // Use your Redis connection string
            options.InstanceName = "AzDoBoardsTokenCache";
        });

        // Add Dependency Injection
        builder.Services.AddSingleton<CacheBuster>();
        builder.Services.AddHttpContextAccessor();
        var organizationUrl = builder.Configuration["AzureDevOps:OrganizationUrl"] ?? string.Empty;
        builder.Services.AddScoped(sp =>
        {
            var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new ConnectionFactory(tokenAcquisition, httpContextAccessor, organizationUrl);
        });
        builder.Services.AddScoped<Projects>(); // Register Projects (which depends on ConnectionFactory)
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));

        // Add Entra ID (Azure AD) Authentication
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi(["499b84ac-1321-427f-aa17-267ca6975798/.default"]) // Acquire tokens for downstream APIs
            .AddDistributedTokenCaches();

        builder.Services.AddAuthorization(options => {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        
        // Add services to the container
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddControllers(); // Add support for controllers and views (starting with sign out)

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Build Application
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (Server.IsProduction)
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.UseStaticFiles();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapControllers(); // Map controller routes

        app.Run();
    }
}
