namespace StatefulSemanticReddit.Domain
{
    public sealed class DetectedLanguage
    {
        public string name { get; set; }
        public string iso6391Name { get; set; }
        public float score { get; set; }
    }
}
