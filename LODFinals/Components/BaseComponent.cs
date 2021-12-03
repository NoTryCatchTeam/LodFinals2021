using System;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using LODFinals.Definitions.Exceptions;
using Microsoft.AspNetCore.Components;

namespace LODFinals.Components
{
    public class BaseComponent : LayoutComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected ISessionStorageService SessionStorageService { get; set; }

        protected string ErrorMessage { get; set; }

        protected async Task WrappLogicAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (HumanReadableException exception)
            {
                ErrorMessage = exception.DisplayMessage;
            }
            catch
            {
                //
            }
        }
    }
}
