using LODFinals.Api.Definitions.RestResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LODFinals.Api.Services
{
    public interface ISolrRestService
    {
        Task<IEnumerable<PressInfo>> GetPressInfoAsync(string username);
       
    }
}
