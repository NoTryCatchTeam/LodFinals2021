using System.Collections.Generic;

namespace LODFinals.Definitions.Models
{
    public class UserData
    {
        public string Name { get; set; }
        public IReadOnlyCollection<ClaimData> Claims { get; set; }
    }
}
