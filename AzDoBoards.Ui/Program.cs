using AzDoBoards.Ui.Components;
using AzDoBoards.Utility;
using Azure.Identity;
using Serilog;

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
        if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets(nameof(AzDoBoards));

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

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddHsts(options => // https://aka.ms/aspnetcore-hsts.
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }

        // Add services to the container
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Build Application
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
