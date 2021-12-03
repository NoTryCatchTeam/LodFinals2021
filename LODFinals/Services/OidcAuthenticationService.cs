using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using LODFinals.Definitions.HttpClients;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace LODFinals.Services
{
    //https://github.com/dotnet/aspnetcore/blob/a450cb69b5e4549f5515cdb057a68771f56cefd7/src/Components/WebAssembly/WebAssembly.Authentication/src/Services/RemoteAuthenticationService.cs
    public class OidcAuthenticationService : RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>
    {
        private const string CODE = "code";
        private readonly OidcHttpClient _oidcClient;
        private readonly OidcClientOptions _oidcClientOptions;
        private readonly ISessionStorageService _sessionStorageService;
        private readonly Regex _regex = new Regex($@"\&code=(?<{CODE}>(\S+))$", RegexOptions.IgnoreCase);

        public OidcAuthenticationService(
            IJSRuntime jsRuntime,
            IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options,
            NavigationManager navigation,
            AccountClaimsPrincipalFactory<RemoteUserAccount> accountClaimsPrincipalFactory,
            OidcHttpClient oidcClient,
            ISessionStorageService sessionStorageService,
            OidcClientOptions oidcClientOptions)
            : base(jsRuntime, options, navigation, accountClaimsPrincipalFactory)
        {
            _oidcClient = oidcClient;
            _oidcClientOptions = oidcClientOptions;
            _sessionStorageService = sessionStorageService;
        }

        public override async Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            var result = await base.CompleteSignOutAsync(context);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                await SignOutAsync();
            }
            return result;
        }

        public override async Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            var result = await base.SignOutAsync(context);
            if (result.Status == RemoteAuthenticationStatus.Success)
            {
                await SignOutAsync();
            }
            return result;
        }

        public override async Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        {
            var result = new RemoteAuthenticationResult<RemoteAuthenticationState>
            {
                Status = RemoteAuthenticationStatus.Failure,
            };

            try
            {
                var disco = await _oidcClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
                {
                    Address = _oidcClientOptions.Authority,
                    Policy = _oidcClientOptions.Policy.Discovery
                }, default);

                var match = _regex.Match(context.Url);

                if (!match.Success)
                {
                    return result;
                }

                var tokenResult = await _oidcClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    RedirectUri = _oidcClientOptions.RedirectUri,
                    ClientId = _oidcClientOptions.ClientId,
                    ClientSecret = _oidcClientOptions.ClientSecret,
                    ClientAssertion = _oidcClientOptions.ClientAssertion,
                    ClientCredentialStyle = _oidcClientOptions.TokenClientCredentialStyle,
                    Code = match.Groups[CODE].ToString(),
                    Parameters = new Parameters()
                }, default);

                if (tokenResult.IsError || tokenResult.HttpStatusCode != HttpStatusCode.OK)
                {
                    result.ErrorMessage = tokenResult.Error;
                    return result;
                }

                // validate token response
                var tokenResponseValidationResult = await ValidateTokenResponseAsync(tokenResult, requireIdentityToken: false, cancellationToken: default);
                if (tokenResponseValidationResult.IsError)
                {
                    result.ErrorMessage = $"Error validating token response: {tokenResponseValidationResult.Error}";
                    return result;
                }

                ClaimsPrincipal principal = tokenResponseValidationResult?.IdentityTokenValidationResult?.User ?? Principal.Create(_oidcClientOptions.Authority);

                var userInfoClaims = Enumerable.Empty<Claim>();
                if (_oidcClientOptions.LoadProfile)
                {
                    var userInfoResult = await GetUserInfoAsync(tokenResult.AccessToken, default);
                    if (userInfoResult.IsError)
                    {
                        result.ErrorMessage = $"Error contacting userinfo endpoint: {userInfoResult.Error}";
                        
                        return result;
                    }

                    userInfoClaims = userInfoResult.Claims;

                    var userInfoSub = userInfoClaims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
                    if (userInfoSub == null)
                    {
                        result.ErrorMessage = "sub claim is missing from userinfo endpoint";

                        return result;
                    }

                    if (tokenResult.IdentityToken != null)
                    {
                        if (!string.Equals(userInfoSub.Value, principal.FindFirst(JwtClaimTypes.Subject).Value))
                        {
                            result.ErrorMessage = "sub claim from userinfo endpoint is different than sub claim from identity token.";

                            return result;
                        }
                    }
                }

                var authTimeValue = principal.FindFirst(JwtClaimTypes.AuthenticationTime)?.Value;
                DateTimeOffset? authTime = null;

                if (IsPresent(authTimeValue) && long.TryParse(authTimeValue, out long seconds))
                {
                    authTime = DateTimeOffset.FromUnixTimeSeconds(seconds);
                }

                var user = ProcessClaims(principal, userInfoClaims);

                await _sessionStorageService.ClearAsync();
                await _sessionStorageService.SetItemAsStringAsync(SessionConstants.ACCESS_TOKEN, tokenResult.AccessToken);
                await _sessionStorageService.SetItemAsStringAsync(SessionConstants.REFRESH_TOKEN, tokenResult.RefreshToken);
                await _sessionStorageService.SetItemAsStringAsync(SessionConstants.USER_DATA, JsonConvert.SerializeObject(user));

                result.Status = RemoteAuthenticationStatus.Success;
                result.State = context.State;

                NotifyAuthenticationStateChanged();
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }


            return result;
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

        public virtual async Task<CustomUserInfoResult> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            if (IsMissing(accessToken)) throw new ArgumentNullException(nameof(accessToken));
            if (!_oidcClientOptions.ProviderInformation.SupportsUserInfo) throw new InvalidOperationException("No userinfo endpoint specified");

            var userInfoResponse = await _oidcClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = _oidcClientOptions.ProviderInformation.UserInfoEndpoint,
                Token = accessToken
            }, cancellationToken).ConfigureAwait(false);

            if (userInfoResponse.IsError)
            {
                return new CustomUserInfoResult
                {
                    Error = userInfoResponse.Error
                };
            }

            return new CustomUserInfoResult
            {
                Claims = userInfoResponse.Claims
            };
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
                if (_oidcClientOptions.IdentityTokenValidator == null)
                {
                    if (_oidcClientOptions.Policy.RequireIdentityTokenSignature == false)
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
                    validator = _oidcClientOptions.IdentityTokenValidator;
                }

                var validationResult = await validator.ValidateAsync(response.IdentityToken, _oidcClientOptions, cancellationToken);

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
                        if (_oidcClientOptions.Policy.RequireAccessTokenHash)
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

public class CustomUserInfoResult : UserInfoResult
{
    public new IEnumerable<Claim> Claims { get; set; }
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

