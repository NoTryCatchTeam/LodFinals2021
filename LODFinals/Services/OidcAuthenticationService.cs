using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;
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
        private const string CODE = "code";
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
                throw new HumanReadableException("Ошибка аутентификации");
            }

            await _sessionStorageService.ClearAsync();
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.ACCESS_TOKEN, loginResult.AccessToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.REFRESH_TOKEN, loginResult.RefreshToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.USER_DATA, JsonConvert.SerializeObject(new UserData
            {
                Name = loginResult.User.Identity.Name ?? "team11",
                Claims = loginResult.User.Claims?.Select(claim => new ClaimData
                {
                    Type = claim.Type,
                    Value = claim.Type,
                })?.ToArray(),
            }));
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
            var user = await _sessionStorageService.GetItemAsStringAsync(SessionConstants.USER_DATA);

            if (user == null)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var userData = JsonConvert.DeserializeObject<UserData>(user);

            var userPrincipal = new ClaimsPrincipal();
            userPrincipal
                .AddIdentity(
                    new ClaimsIdentity(userData.Claims?.Select(claim => new Claim(claim.Type, claim.Value))));

            return userPrincipal;

        }
    }
}

