using System.Collections.Generic;
using Newtonsoft.Json;

namespace SemanticRedditCore.Domain
{
    public class SentimentQueryObject
    {
        [JsonProperty("documents")]
        public IList<SentimentQueryDocument> Documents { get; set; }
    }
}