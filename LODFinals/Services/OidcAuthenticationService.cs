using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;
using LODFinals.Definitions.Constants;
using LODFinals.Definitions.Exceptions;
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
        private readonly Regex _regex = new Regex($@"\#code=(?<{CODE}>(\S+))\&", RegexOptions.IgnoreCase);

        public OidcAuthenticationService(
            ISessionStorageService sessionStorageService,
            OidcClient oidcClient,
            NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            _oidcClient = oidcClient;
            _sessionStorageService = sessionStorageService;
        }

        protected OidcClientOptions Options => _oidcClient.Options;

        public Task StartLoginAsync()
            => _oidcClient.LoginAsync(new LoginRequest());

        public async Task FinishLoginAsync(string callbackUrl)
        {
            var loginResult = await _oidcClient.ProcessResponseAsync(callbackUrl, new AuthorizeState
            {
                State = new AuthorizeResponse(callbackUrl).State,
                RedirectUri = Options.RedirectUri,
                CodeVerifier = CryptoRandom.CreateUniqueId(),
            });

            if (loginResult.IsError)
            {
                throw new HumanReadableException("Ошибка аутентификации");
            }

            await _sessionStorageService.ClearAsync();
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.ACCESS_TOKEN, loginResult.AccessToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.REFRESH_TOKEN, loginResult.RefreshToken);
            await _sessionStorageService.SetItemAsStringAsync(SessionConstants.USER_DATA, JsonConvert.SerializeObject(loginResult.User));

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

            return JsonConvert.DeserializeObject<ClaimsPrincipal>(user);
        }

        internal ClaimsPrincipal ProcessClaims(ClaimsPrincipal user, IEnumerable<Claim> userInfoClaims)
        {
            var combinedClaims = new HashSet<Claim>(new ClaimComparer(new ClaimComparer.Options { IgnoreIssuer = true }));

            user.Claims.ToList().ForEach(c => combinedClaims.Add(c));
            userInfoClaims.ToList().ForEach(c => combinedClaims.Add(c));

            List<Claim> userClaims = combinedClaims.ToList();

            return new ClaimsPrincipal(new ClaimsIdentity(userClaims, user.Identity.AuthenticationType, user.Identities.First().NameClaimType, user.Identities.First().RoleClaimType));
        }

        internal async Task<TokenResponseValidationResult> ValidateTokenResponseAsync(TokenResponse response, bool requireIdentityToken, CancellationToken cancellationToken = default)
        {
            // token response must contain an access token
            if (IsMissing(response.AccessToken))
            {
                return new TokenResponseValidationResult("Access token is missing on token response.");
            }

            if (requireIdentityToken)
            {
                // token response must contain an identity token (openid scope is mandatory)
                if (IsMissing(response.IdentityToken))
                {
                    return new TokenResponseValidationResult("Identity token is missing on token response.");
                }
            }

            if (IsPresent(response.IdentityToken))
            {
                IIdentityTokenValidator validator;
                if (Options.IdentityTokenValidator == null)
                {
                    if (Options.Policy.RequireIdentityTokenSignature == false)
                    {
                        validator = new NoValidationIdentityTokenValidator();
                    }
                    else
                    {
                        throw new InvalidOperationException("No IIdentityTokenValidator is configured. Either explicitly set a validator on the options, or set OidcClientOptions.Policy.RequireIdentityTokenSignature to false to skip validation.");
                    }
                }
                else
                {
                    validator = Options.IdentityTokenValidator;
                }

                var validationResult = await validator.ValidateAsync(response.IdentityToken, Options, cancellationToken);

                if (validationResult.IsError)
                {
                    return new TokenResponseValidationResult(validationResult.Error ?? "Identity token validation error");
                }

                // validate at_hash
                if (!string.Equals(validationResult.SignatureAlgorithm, "none", StringComparison.OrdinalIgnoreCase))
                {
                    var atHash = validationResult.User.FindFirst(JwtClaimTypes.AccessTokenHash);
                    if (atHash == null)
                    {
                        if (Options.Policy.RequireAccessTokenHash)
                        {
                            return new TokenResponseValidationResult("at_hash is missing.");
                        }
                    }
                    else
                    {
                        if (!ValidateHash(response.AccessToken, atHash.Value, validationResult.SignatureAlgorithm))
                        {
                            return new TokenResponseValidationResult("Invalid access token hash.");
                        }
                    }
                }

                return new TokenResponseValidationResult(validationResult);
            }

            return new TokenResponseValidationResult((IdentityTokenValidationResult)null);
        }

        static bool IsMissing(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        static bool IsPresent(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public bool ValidateHash(string data, string hashedData, string signatureAlgorithm)
        {
            var hashAlgorithm = GetMatchingHashAlgorithm(signatureAlgorithm);


            using (hashAlgorithm)
            {
                var hash = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(data));
                var size = (hashAlgorithm.HashSize / 8) / 2;

                byte[] leftPart = new byte[hashAlgorithm.HashSize / size];
                Array.Copy(hash, leftPart, hashAlgorithm.HashSize / size);

                var leftPartB64 = Base64Url.Encode(leftPart);
                return leftPartB64.Equals(hashedData);
            }
        }

        public HashAlgorithm GetMatchingHashAlgorithm(string signatureAlgorithm)
        {
            var signingAlgorithmBits = int.Parse(signatureAlgorithm.Substring(signatureAlgorithm.Length - 3));

            switch (signingAlgorithmBits)
            {
                case 256:
                    return SHA256.Create();
                case 384:
                    return SHA384.Create();
                case 512:
                    return SHA512.Create();
                default:
                    return null;
            }
        }
    }
}

public class TokenResponseValidationResult : Result
{
    public TokenResponseValidationResult(string error)
    {
        Error = error;
    }

    public TokenResponseValidationResult(IdentityTokenValidationResult result)
    {
        IdentityTokenValidationResult = result;
    }

    public virtual IdentityTokenValidationResult IdentityTokenValidationResult { get; set; }
}

