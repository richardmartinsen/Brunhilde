using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Brunhilde.Domain
{
    public sealed class RedditComment
    {
        public int UpWotes { get; set; }
        public int DownVotes { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedUTC { get; set; }
        public string Id { get; set; }
    }
}
