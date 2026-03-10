using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AzDoBoards.Ui;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

if (builder.HostEnvironment.IsDevelopment())
{
    // Load local developer secrets (not checked in), overriding placeholder values in appsettings.json.
    using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    using var response = await http.GetAsync("appsettings.Secrets.json");
    if (response.IsSuccessStatusCode)
        builder.Configuration.AddJsonStream(await response.Content.ReadAsStreamAsync());
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    // Request an Azure DevOps access token via user impersonation. This token will later be used to call the Azure DevOps REST API or the TFS Client library
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation");

    // Use redirect (not popup) so the browser navigates to Entra ID on first visit
    options.ProviderOptions.LoginMode = "redirect";
});

await builder.Build().RunAsync();
