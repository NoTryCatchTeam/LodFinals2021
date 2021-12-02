using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using LODFinals.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LODFinals
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services
                .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                .AddBlazoredSessionStorage(config =>
                {
                    config.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    config.JsonSerializerOptions.IgnoreNullValues = true;
                    config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                    config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    config.JsonSerializerOptions.WriteIndented = false;
                })
                .AddScoped<AuthenticationStateProvider>()
                .AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthenticationStateProvider>())
                .AddOptions()
                .AddAuthorizationCore();

            await builder.Build().RunAsync();
        }
    }
}
