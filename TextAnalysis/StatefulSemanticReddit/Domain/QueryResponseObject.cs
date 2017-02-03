namespace StatefulSemanticReddit.Domain
{
    public class QueryResponseObject
    {
        public Document[] documents { get; set; }
        public object[] errors { get; set; }
    }
}