using System.Collections.Generic;
using Newtonsoft.Json;

namespace CSHttpClientSample
{
    public class SentimentQueryObject
    {
        [JsonProperty("documents")]
        public IList<SentimentQueryDocument> Documents { get; set; }
    }
}