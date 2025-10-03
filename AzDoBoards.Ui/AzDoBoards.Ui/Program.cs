using AzDoBoards.Ui.Client.Pages;
using AzDoBoards.Ui.Components;
using AzDoBoards.Utility;
using Azure.Identity;
using Serilog;

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

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

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
