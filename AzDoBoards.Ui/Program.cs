using AzDoBoards.Client;
using AzDoBoards.Client.Services;
using AzDoBoards.Data;
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

        if (Server.IsDevelopment)
            builder.Configuration.AddUserSecrets(nameof(AzDoBoards)); // Add support for secrets.json for development

        // Add Serilog Support
        SerilogHelper.Configure(builder.Environment, builder.Configuration);
        builder.Host.UseSerilog(); // replace logging with Serilog

        // Add Azure KeyVault configuration
        var keyVaultEndpoint = Environment.GetEnvironmentVariable(Utility.Constants.Azure_EnvcfgKeyVaultEndpoint);
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
            builder.Services.AddHsts(options => // https://aka.ms/aspnetcore-hsts
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(Utility.Constants.Security_HstsMaxAgeDays);
            });
        }

        // Add Mudblazor
        builder.Services.AddMudServices();

        // Add Redis Cache
        var redisConnectionString = builder.Configuration[Utility.Constants.Redis_ConfigConnectionString] ?? Utility.Constants.Redis_DefaultConnectionString;
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString; // Use your Redis connection string
            options.InstanceName = Utility.Constants.Redis_TokenCacheInstanceName;
        });

        // Add Data Services (Settings Repository)
        var storageConnectionString = builder.Configuration[Utility.Constants.Azure_ConfigStorageConnectionString] ?? Utility.Constants.Azure_DefaultStorageConnectionString;
        builder.Services.AddDataServices(storageConnectionString);

        // Add Dependency Injection
        builder.Services.AddSingleton<CacheBuster>();
        builder.Services.AddHttpContextAccessor();
        var organizationUrl = builder.Configuration[Utility.Constants.AzureDevOps_ConfigOrganizationUrl] ?? string.Empty;
        builder.Services.AddScoped(sp =>
        {
            var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new ConnectionFactory(tokenAcquisition, httpContextAccessor, organizationUrl);
        });
        builder.Services.AddScoped<ProjectServices>(); // Register Projects (which depends on ConnectionFactory)
        builder.Services.AddScoped<ProcessServices>(); // Register Process (which depends on ConnectionFactory)
        builder.Services.AddScoped<WorkItemStateServices>(); // Register Work Item State Services (which depends on ConnectionFactory)
        builder.Services.AddScoped<WorkItemServices>(); // Register Work Items (which depends on ConnectionFactory)
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString)); // Register Redis connection multiplexer
        builder.Services.AddScoped<Services.HierarchyService>();
        builder.Services.AddScoped<Services.RoadmapService>(); // Register Roadmap Service

        // Add Entra ID (Azure AD) Authentication
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options =>
            {
                builder.Configuration.GetSection(Utility.Constants.Azure_ConfigSection).Bind(options);
                
                // Configure OpenID Connect events for account selection
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // Check if we have a prompt parameter in the authentication properties
                        if (context.Properties.Parameters.TryGetValue("prompt", out var promptValue))
                        {
                            context.ProtocolMessage.Prompt = promptValue?.ToString();
                        }
                        
                        // Optionally add domain_hint for work/school accounts
                        // Uncomment the following lines if you want to hint that work accounts are preferred
                        // if (!context.ProtocolMessage.Parameters.ContainsKey("domain_hint"))
                        // {
                        //     context.ProtocolMessage.DomainHint = "organizations";
                        // }
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Log authentication failures for debugging
                        Log.Error("Authentication failed: {Error}", context.Exception?.Message);
                        return Task.CompletedTask;
                    }
                };
            })
            .EnableTokenAcquisitionToCallDownstreamApi([Utility.Constants.AzureDevOps_OAuthScope]) // Acquire tokens for downstream APIs
            .AddDistributedTokenCaches();

        builder.Services.AddAuthorization(options => {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        // Add anti-forgery services first
        builder.Services.AddAntiforgery();

        // Add services to the container - order matters for Blazor + MVC
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add Controllers with Views (includes anti-forgery support)
        builder.Services.AddControllersWithViews();

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

        app.UseStatusCodePagesWithReExecute(Utility.Constants.Page_FoundFoundPath);

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