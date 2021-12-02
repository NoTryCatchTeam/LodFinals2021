using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;

namespace LODFinals.Components
{
    public class BaseComponent : LayoutComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected ISessionStorageService SessionStorageService { get; set; }
    }
}
