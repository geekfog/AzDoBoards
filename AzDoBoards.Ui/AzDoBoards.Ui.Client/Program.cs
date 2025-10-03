using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using AzDoBoards.Ui.Client.Services;

namespace AzDoBoards.Ui.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Add MudBlazor services
            builder.Services.AddMudServices();
            
            // Add custom services
            builder.Services.AddSingleton<ThemeService>();

            await builder.Build().RunAsync();
        }
    }
}
