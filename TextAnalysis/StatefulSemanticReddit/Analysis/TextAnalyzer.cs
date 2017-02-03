using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StatefulSemanticReddit.Domain;

namespace StatefulSemanticReddit.Analysis
{
    public abstract class TextAnalyzer
    {
        public async Task DoAnalysis(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch)
        {
            using (HttpClient client = new HttpClient())
            {
                // Request headers
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Config.OcpApimSubscriptionKey);

                //Send request
                StringContent content = GetQueryContent(redditCommentBatch);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                //Get answer
                HttpResponseMessage response = await client.PostAsync(Url, content);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                ProcessQueryResponse(redditCommentBatch, jsonResponse);

                response.Dispose();
            }
        }

        protected abstract void ProcessQueryResponse(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch, string jsonResponse);
        protected abstract StringContent GetQueryContent(IReadOnlyDictionary<string, AnalysedRedditComment> redditCommentBatch);
        public abstract string Url { get; }
    }
}
