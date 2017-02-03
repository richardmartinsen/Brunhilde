using Newtonsoft.Json;

namespace StatefulSemanticReddit.Domain
{
    public class QueryDocument
    {
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}