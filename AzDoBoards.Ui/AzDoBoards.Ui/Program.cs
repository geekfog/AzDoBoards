using AzDoBoards.Ui.Client.Pages;
using AzDoBoards.Ui.Components;
using AzDoBoards.Utility;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using MudBlazor.Services;
using Serilog;
using StackExchange.Redis;
using AzDoBoards.Ui.Client.Services;

namespace AzDoBoards.Ui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Build Services
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            var builder = WebApplication.CreateBuilder(args);

            // Add support for secrets.json for development
            if (Server.IsDevelopment)
                builder.Configuration.AddUserSecrets(nameof(AzDoBoards));

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

            // Add HSTS for production
            if (Server.IsProduction)
            {
                builder.Services.AddHsts(options => // https://aka.ms/aspnetcore-hsts
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(Utility.Constants.Security_HstsMaxAgeDays);
                });
            }

            // Add Redis Cache
            var redisConnectionString = builder.Configuration[Utility.Constants.Redis_ConfigConnectionString] ?? Utility.Constants.Redis_DefaultConnectionString;
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString; // Use your Redis connection string
                options.InstanceName = Utility.Constants.Redis_TokenCacheInstanceName;
            });

            // Add Dependency Injection
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString)); // Register Redis connection multiplexer

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

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            // Add MudBlazor services
            builder.Services.AddMudServices();
            
            // Add custom services (needed for server-side rendering)
            builder.Services.AddSingleton<ThemeService>();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Build Application
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            // Add Entra ID (Azure AD) Authentication
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
