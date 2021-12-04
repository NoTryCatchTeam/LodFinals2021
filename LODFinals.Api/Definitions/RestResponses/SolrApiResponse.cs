namespace LODFinals.Api.Definitions.RestResponses
{
    public class SolrApiResponse<T>
    {
        public ResponseHeader responseHeader { get; set;}
        public int status { get; set;}  

        public int QTime { get; set;}   

        public Response<T> response { get; set; }
      
    }
}
