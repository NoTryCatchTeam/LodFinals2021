using System.Threading;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;
using Microsoft.AspNetCore.Components;

namespace LODFinals.Services
{
    public class WebAuthenticatorBrowser : IBrowser
    {
        private readonly NavigationManager _navigationManager;

        public WebAuthenticatorBrowser(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            _navigationManager.NavigateTo(options.StartUrl);
            return Task.FromResult(new BrowserResult());
        }
    }
}
