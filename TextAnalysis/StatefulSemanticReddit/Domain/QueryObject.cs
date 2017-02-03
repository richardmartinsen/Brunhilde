using System.Collections.Generic;
using Newtonsoft.Json;

namespace StatefulSemanticReddit.Domain
{
    public class QueryObject
    {
        [JsonProperty("documents")]
        public IList<QueryDocument> Documents { get; set; }
    }
}