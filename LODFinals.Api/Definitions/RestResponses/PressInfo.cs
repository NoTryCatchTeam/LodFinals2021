using System.Collections.Generic;

namespace LODFinals.Api.Definitions.RestResponses
{
    public class PressInfo
    {
        public IEnumerable<string> title {get;set;}
        public IEnumerable<string> text { get;set;}
        
        public IEnumerable<string> source { get;set;}
    }
}
