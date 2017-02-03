namespace SemanticRedditCore.Domain
{
    public class SentimentResponseObject
    {
        public Document[] documents { get; set; }
        public object[] errors { get; set; }
    }
}