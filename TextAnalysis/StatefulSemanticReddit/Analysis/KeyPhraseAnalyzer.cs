using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StatefulSemanticReddit.Domain;

namespace StatefulSemanticReddit.Analysis
{
    public sealed class KeyPhraseAnalyzer : TextAnalyzer
    {
        protected override void ProcessQueryResponse(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch, string jsonResponse)
        {
            QueryResponseObject resp = JsonConvert.DeserializeObject<QueryResponseObject>(jsonResponse);

            foreach (Document document in resp.documents)
            {
                AnalysedRedditComment comment;

                if (redditCommentBatch.TryGetValue(document.id, out comment))
                    comment.KeyPhrases = document.keyPhrases.ToArray();
            }
        }

        protected override StringContent GetQueryContent(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch)
        {
            List<QueryDocument> batch = redditCommentBatch
                .Select(rc => new QueryDocument { Id = rc.Key, Text = rc.Value.Comment, Language = rc.Value.Language ?? "en" })
                .ToList();

            QueryObject queryobject = new QueryObject { Documents = batch };
            StringContent content = new StringContent(JsonConvert.SerializeObject(queryobject));
            return content;
        }

        public override string Url => "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";
    }
}
