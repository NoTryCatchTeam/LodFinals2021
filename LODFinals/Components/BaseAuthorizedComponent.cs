using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LODFinals.Components
{
    [Authorize]
    public class BaseAuthorizedComponent : BaseComponent
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthenticationStateTask { get; set; }
    }
}
