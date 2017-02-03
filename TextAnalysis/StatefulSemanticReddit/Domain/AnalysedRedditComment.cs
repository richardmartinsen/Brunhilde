using System;

// ReSharper disable InconsistentNaming

namespace StatefulSemanticReddit.Domain
{
    public sealed class AnalysedRedditComment
    {
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedUTC { get; set; }
        public string Id { get; set; }
        public string[] KeyPhrases { get; set; }
        public float Sentiment { get; set; }
        public string Language { get; set; }
    }
}
