using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using LODFinals.Definitions.Constants;
using LODFinals.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LODFinals
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents
                .Add<App>("#app");

            builder.Services
                .AddScoped(services => new HttpClient { BaseAddress = new Uri(builder.Configuration.GetValue<string>(ConfigurationConstants.Urls.PRIMARY_URL)) })
                .AddBlazoredLocalStorage(config =>
                {
                    config.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    config.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                    config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    config.JsonSerializerOptions.WriteIndented = false;
                })
                .AddScoped(services =>
                {
                    var opt = new OidcClientOptions
                    {
                        Browser = new WebAuthenticatorBrowser(services.GetRequiredService<NavigationManager>(), services.GetRequiredService<ILocalStorageService>()),
                        Policy = new Policy { Discovery = new DiscoveryPolicy { RequireHttps = false } },
                    };
                    builder.Configuration.Bind(ConfigurationConstants.Authentication.AUTHENTICATION, opt);
                    return new OidcClient(opt);
                })
                .AddScoped<OidcAuthenticationService>()
                .AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<OidcAuthenticationService>())
                .AddScoped<AuthenticationMessageHandler>()
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
                .AddOidcAuthentication(opt => builder.Configuration.Bind(ConfigurationConstants.Authentication.AUTHENTICATION, opt.ProviderOptions));

            await builder.Build().RunAsync();
        }
    }
}
