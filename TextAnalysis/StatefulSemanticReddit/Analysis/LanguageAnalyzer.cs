using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using StatefulSemanticReddit.Domain;

namespace StatefulSemanticReddit.Analysis
{
    public sealed class LanguageAnalyzer : TextAnalyzer
    {
        public override string Url => "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/languages";
        protected override void ProcessQueryResponse(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch, string jsonResponse)
        {
            QueryResponseObject resp = JsonConvert.DeserializeObject<QueryResponseObject>(jsonResponse);

            foreach (Document document in resp.documents
                .Where(d => d.detectedLanguages != null && d.detectedLanguages.Any()))
            {
                AnalysedRedditComment comment;

                if (redditCommentBatch.TryGetValue(document.id, out comment))
                    comment.Language = document.detectedLanguages[0].iso6391Name;
            }

        }

        protected override StringContent GetQueryContent(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch)
        {
            List<QueryDocument> batch = redditCommentBatch
                .Select(rc => new QueryDocument { Id = rc.Key, Text = rc.Value.Comment })
                .ToList();

            QueryObject queryobject = new QueryObject { Documents = batch };
            StringContent content = new StringContent(JsonConvert.SerializeObject(queryobject));
            return content;
        }
    }
}
