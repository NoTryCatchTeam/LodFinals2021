using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using LODFinals.Definitions.Constants;
using LODFinals.Definitions.HttpClients;
using LODFinals.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
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
                .AddHttpClient(builder.Configuration.GetValue<string>(ConfigurationConstants.Urls.API_FLAG), opt => opt.BaseAddress = new Uri(builder.Configuration.GetValue<string>(ConfigurationConstants.Urls.PRIMARY_URL)))
                .AddHttpMessageHandler<AuthenticationMessageHandler>();

            builder.Services
                .AddScoped(services =>
                {
                    var opt = new OidcClientOptions
                    {
                        ClientSecret = builder.Configuration.GetValue<string>(ConfigurationConstants.Authentication.CLIENT_SECRET),
                        Policy = new Policy { Discovery = new DiscoveryPolicy { RequireHttps = false } },
                    };

                    builder.Configuration.Bind(ConfigurationConstants.Authentication.AUTHENTICATION, opt);
                    return opt;
                })
                .AddScoped<OidcHttpClient>()
                .AddScoped<OidcAuthenticationService>()
                .AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(provider => provider.GetRequiredService<OidcAuthenticationService>())
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
