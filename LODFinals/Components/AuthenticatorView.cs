using System;
using System.Threading.Tasks;
using LODFinals.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace LODFinals.Components
{
    // https://github.com/dotnet/aspnetcore/blob/a450cb69b5e4549f5515cdb057a68771f56cefd7/src/Components/WebAssembly/WebAssembly.Authentication/src/RemoteAuthenticatorViewCore.cs#L241
    public class AuthenticatorView : RemoteAuthenticatorView
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected OidcAuthenticationService AuthenticationStateProvider { get; set; }

        [Inject]
        protected IJSRuntime JS { get; set; }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
#if DEBUG
            await Task.Delay(5000);
#endif
            switch (Action)
            {
                case RemoteAuthenticationActions.LogInCallback:
                    await ProcessLogInCallbackAsync();
                    return;
                case RemoteAuthenticationActions.LogOut:
                    await ProcessLogOutAsync(GetReturnUrl(state: null, NavigationManager.ToAbsoluteUri(ApplicationPaths.LogOutSucceededPath).AbsoluteUri));
                    return;
                case RemoteAuthenticationActions.LogOutCallback:
                    await ProcessLogOutCallbackAsync();
                    return;
                case RemoteAuthenticationActions.LogOutSucceeded:
                    await AuthenticationStateProvider.SignOutAsync();
                    return;
            }

            await base.OnParametersSetAsync();
        }

        private async Task ProcessLogOutCallbackAsync()
        {
            var result = await AuthenticationStateProvider.CompleteSignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = NavigationManager.Uri });
            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    // There should not be any redirects as the only time completeAuthentication finishes
                    // is when we are doing a redirect sign in flow.
                    throw new InvalidOperationException("Should not redirect.");
                case RemoteAuthenticationStatus.Success:
                    await OnLogOutSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrlAsync(GetReturnUrl(result.State, NavigationManager.ToAbsoluteUri(ApplicationPaths.LogOutSucceededPath).ToString()));
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                case RemoteAuthenticationStatus.Failure:
                    var uri = NavigationManager.ToAbsoluteUri($"{ApplicationPaths.LogOutFailedPath}?message={Uri.EscapeDataString(result.ErrorMessage)}").ToString();
                    await NavigateToReturnUrlAsync(uri);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid authentication result status.");
            }
        }

        private async Task ProcessLogOutAsync(string returnUrl)
        {
            AuthenticationState.ReturnUrl = returnUrl;

            var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var isauthenticated = state.User.Identity.IsAuthenticated;
            if (isauthenticated)
            {
                var result = await AuthenticationStateProvider.SignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { State = AuthenticationState });
                switch (result.Status)
                {
                    case RemoteAuthenticationStatus.Redirect:
                        break;
                    case RemoteAuthenticationStatus.Success:
                        await OnLogOutSucceeded.InvokeAsync(result.State);
                        await NavigateToReturnUrlAsync(returnUrl);
                        break;
                    case RemoteAuthenticationStatus.OperationCompleted:
                        break;
                    case RemoteAuthenticationStatus.Failure:
                        NavigationManager.NavigateTo(ApplicationPaths.LogOutFailedPath);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid authentication result status.");
                }
            }
            else
            {
                await NavigateToReturnUrlAsync(returnUrl);
            }
        }

        // https://github.com/dotnet/aspnetcore/blob/a450cb69b5e4549f5515cdb057a68771f56cefd7/src/Components/WebAssembly/WebAssembly.Authentication/src/RemoteAuthenticatorViewCore.cs#L241
        private async Task ProcessLogInCallbackAsync()
        {
            var url = NavigationManager.Uri;
            var result = await AuthenticationStateProvider.CompleteSignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState> { Url = url });
            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    throw new InvalidOperationException("Should not redirect.");
                case RemoteAuthenticationStatus.Success:
                    await OnLogInSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrlAsync(GetReturnUrl(result.State));
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                case RemoteAuthenticationStatus.Failure:
                    var uri = NavigationManager.ToAbsoluteUri($"{ApplicationPaths.LogInFailedPath}?message={Uri.EscapeDataString(result.ErrorMessage)}").ToString();
                    await NavigateToReturnUrlAsync(uri);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid authentication result status '{result.Status}'.");
            }
        }

        private async Task NavigateToReturnUrlAsync(string returnUrl) => await JS.InvokeVoidAsync("Blazor.navigateTo", returnUrl, false, true);

        private string GetReturnUrl(RemoteAuthenticationState state, string defaultReturnUrl = null)
        {
            if (state?.ReturnUrl != null)
            {
                return state.ReturnUrl;
            }

            var fromQuery = GetParameter(new Uri(NavigationManager.Uri).Query, "returnUrl");
            if (!string.IsNullOrWhiteSpace(fromQuery) && !fromQuery.StartsWith(NavigationManager.BaseUri, StringComparison.Ordinal))
            {
                // This is an extra check to prevent open redirects.
                throw new InvalidOperationException("Invalid return url. The return url needs to have the same origin as the current page.");
            }

            return fromQuery ?? defaultReturnUrl ?? NavigationManager.BaseUri;

            #region copied code
            // https://github.com/dotnet/aspnetcore/blob/a450cb69b5e4549f5515cdb057a68771f56cefd7/src/Components/WebAssembly/WebAssembly.Authentication/src/QueryStringHelper.cs
            static string GetParameter(string queryString, string key)
            {
                if (string.IsNullOrEmpty(queryString) || queryString == "?")
                {
                    return null;
                }

                var scanIndex = 0;
                if (queryString[0] == '?')
                {
                    scanIndex = 1;
                }

                var textLength = queryString.Length;
                var equalIndex = queryString.IndexOf('=');
                if (equalIndex == -1)
                {
                    equalIndex = textLength;
                }

                while (scanIndex < textLength)
                {
                    var ampersandIndex = queryString.IndexOf('&', scanIndex);
                    if (ampersandIndex == -1)
                    {
                        ampersandIndex = textLength;
                    }

                    if (equalIndex < ampersandIndex)
                    {
                        while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                        {
                            ++scanIndex;
                        }
                        var name = queryString[scanIndex..equalIndex];
                        var value = queryString.Substring(equalIndex + 1, ampersandIndex - equalIndex - 1);
                        var processedName = Uri.UnescapeDataString(name.Replace('+', ' '));
                        if (string.Equals(processedName, key, StringComparison.OrdinalIgnoreCase))
                        {
                            return Uri.UnescapeDataString(value.Replace('+', ' '));
                        }

                        equalIndex = queryString.IndexOf('=', ampersandIndex);
                        if (equalIndex == -1)
                        {
                            equalIndex = textLength;
                        }
                    }
                    else
                    {
                        if (ampersandIndex > scanIndex)
                        {
                            var value = queryString[scanIndex..ampersandIndex];
                            if (string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                            {
                                return string.Empty;
                            }
                        }
                    }

                    scanIndex = ampersandIndex + 1;
                }

                return null;
            }
            #endregion
        }
    }
}
