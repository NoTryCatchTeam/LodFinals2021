using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using LODFinals.Definitions.Constants;
using LODFinals.Definitions.Exceptions;
using LODFinals.Definitions.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;

namespace LODFinals.Services
{
    public class OidcAuthenticationService : AuthenticationStateProvider
    {
        private readonly NavigationManager _navigationManager;
        private readonly OidcClient _oidcClient;
        private readonly ISessionStorageService _sessionStorageService;
        private readonly ILocalStorageService _localStorageService;

        public OidcAuthenticationService(
            ISessionStorageService sessionStorageService,
            OidcClient oidcClient,
            NavigationManager navigationManager,
            ILocalStorageService localStorageService)
        {
            _navigationManager = navigationManager;
            _oidcClient = oidcClient;
            _sessionStorageService = sessionStorageService;
            _localStorageService = localStorageService;
        }

        protected OidcClientOptions Options => _oidcClient.Options;

        public Task StartLoginAsync()
            => _oidcClient.LoginAsync(new LoginRequest());

        public async Task FinishLoginAsync(string callbackUrl)
        {
            var codeVerifier = await _localStorageService.GetItemAsStringAsync(SessionConstants.CODE_VERIFIER);
            if (codeVerifier == null)
            {
                await LogoutAsync();
                _navigationManager.NavigateTo("/auth/login");
                return;
            }

            await _localStorageService.ClearAsync();

            var loginResult = await _oidcClient.ProcessResponseAsync(callbackUrl, new AuthorizeState
            {
                State = new AuthorizeResponse(callbackUrl).State,
                RedirectUri = Options.RedirectUri,
                CodeVerifier = codeVerifier,
            });

            if (loginResult.IsError)
            {
                await LogoutAsync();
                throw new HumanReadableException("Ошибка аутентификации");
            }

            await _sessionStorageService.ClearAsync();
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.ACCESS_TOKEN, loginResult.AccessToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.REFRESH_TOKEN, loginResult.RefreshToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.IDENTITY_TOKEN, loginResult.IdentityToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.USER_NAME, loginResult.User.Identity.Name ?? "team11");

            NotifyAuthenticationStateChanged();
            _navigationManager.NavigateTo("/");
        }

        public async Task LogoutAsync()
        {
            var logoutResult = await _oidcClient.LogoutAsync(new LogoutRequest());

            if (logoutResult.IsError)
            {
                throw new HumanReadableException($"Ошибка выхода из профиля: {logoutResult.ErrorDescription}");
            }

            await SignOutAsync();
        }

        public void NotifyAuthenticationStateChanged()
            => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
            => new AuthenticationState(await GetUserAsync());

        public ValueTask SignOutAsync()
            => _sessionStorageService.ClearAsync();

        private async Task<ClaimsPrincipal> GetUserAsync()
        {
// #if DEBUG
//             var identity = new ClaimsIdentity(new[]
//                 {
//                     new Claim(ClaimTypes.Name, "mrfibuli"),
//                 }, "Fake authentication type");
//
//             var user = new ClaimsPrincipal(identity);
//
//             return user;
// #endif

            var accessToken = await _sessionStorageService.GetItemAsStringAsync(SessionConstants.ACCESS_TOKEN);
            var identityToken = await _sessionStorageService.GetItemAsStringAsync(SessionConstants.IDENTITY_TOKEN);

            if (accessToken == null
                || identityToken == null)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            // validate token response
            var tokenResponseValidationResult = await _oidcClient.Processor.ValidateTokenResponseAsync(accessToken, identityToken, requireIdentityToken: false);
            if (tokenResponseValidationResult.IsError)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            return tokenResponseValidationResult?.IdentityTokenValidationResult?.User ?? Principal.Create(_oidcClient.Options.Authority);
        }
    }
}

