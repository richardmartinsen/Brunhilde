using Newtonsoft.Json;

namespace CSHttpClientSample
{
    public class SentimentQueryDocument
    {
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}