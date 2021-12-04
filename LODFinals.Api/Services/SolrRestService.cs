using LODFinals.Api.Definitions.RestResponses;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LODFinals.Api.Services
{
    public class SolrRestService : ISolrRestService
    {
        private IHttpClientFactory _clientFactory;

        public SolrRestService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IEnumerable<PressInfo>> GetPressInfoAsync(string username)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
            "http://45.134.255.154:30083/solr/dud/select?indent=true&q.op=OR&q=*%3A*&qt={username}");

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);
            using var responseStream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync
                <IEnumerable<PressInfo>>(responseStream);

            return result;
        }
    }
}
