using System;

namespace StatefulSemanticReddit.Domain
{
    public sealed class RedditComment
    {
        public int UpWotes { get; set; }
        public int DownVotes { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedUTC { get; set; }
        public string Id { get; set; }
        public string SubReddit { get; set; }

    }
}
