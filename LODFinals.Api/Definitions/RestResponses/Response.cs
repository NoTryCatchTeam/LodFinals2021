using System.Collections.Generic;

namespace LODFinals.Api.Definitions.RestResponses
{
    public class Response<T>
    {
        public int numFound { get; set; }

        public IEnumerable<T> docs { get; set; }
    }
}