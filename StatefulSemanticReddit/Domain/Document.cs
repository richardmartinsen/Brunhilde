using System.Collections.Generic;

namespace StatefulSemanticReddit.Domain
{
    public class Document
    {
        public float score { get; set; }
        public string id { get; set; }
        public List<DetectedLanguage> detectedLanguages { get; set; }
        public List<string> keyPhrases { get; set; }
    }
}