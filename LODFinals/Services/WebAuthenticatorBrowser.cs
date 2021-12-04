using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using IdentityModel.OidcClient.Browser;
using LODFinals.Definitions.Constants;
using Microsoft.AspNetCore.Components;

namespace LODFinals.Services
{
    public class WebAuthenticatorBrowser : IBrowser
    {
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorageService;

        public WebAuthenticatorBrowser(NavigationManager navigationManager, ILocalStorageService localStorageService)
        {
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            if (options.CodeVerifier != null)
            {
                await _localStorageService.SetItemAsStringAsync(SessionConstants.CODE_VERIFIER, options.CodeVerifier);
            }
            _navigationManager.NavigateTo(options.StartUrl);
            return new BrowserResult();
        }
    }
}
