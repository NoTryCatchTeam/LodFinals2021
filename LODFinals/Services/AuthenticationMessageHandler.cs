using LODFinals.Definitions.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace LODFinals.Services
{
    public class AuthenticationMessageHandler : AuthorizationMessageHandler
    {
        public AuthenticationMessageHandler(IAccessTokenProvider provder, NavigationManager nav, IConfiguration configuration)
            : base(provder, nav)
            => ConfigureHandler(new string[] { configuration.GetValue<string>(ConfigurationConstants.Urls.PRIMARY_URL) });
    }
}