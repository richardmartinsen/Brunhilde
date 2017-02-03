using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Brunhilde.Domain;
using Brunhilde.EventInput;
using Newtonsoft.Json;
using Microsoft.ServiceBus.Messaging;

namespace CSHttpClientSample
{
    static class Program
    {
        private const string EhConnectionString = "Endpoint=sb://dev-brunhilde.servicebus.windows.net/;SharedAccessKeyName=reddit;SharedAccessKey=de0fz35s+6BQ0HmYmqf4lUI8R+EsmKxZrLkCaA2O7E4=";
        private const string EhAnalyticPath = "analytics";
        private const string EhCommentPath = "reddit-comments";
        private const string StorageContainerName = "{Storage account container name}";
        private const string StorageAccountName = "{Storage account name}";
        private const string StorageAccountKey = "{Storage account key}";
        private static readonly string StorageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        private const string OffsetFileName = @".\offset.txt";

        public static void Main()
        {
            EventReader reader = new EventReader(EhConnectionString, EhCommentPath, OffsetFileName);

            IEnumerable<RedditComment> comments = reader.GetComments("$Default", "1", DateTime.MinValue);

            ProcessMessages(comments);

            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
            
        }


        public static async void ProcessMessages(IEnumerable<RedditComment> comments)
        {
            EventHubClient publishClient = EventHubClient.CreateFromConnectionString(EhConnectionString, EhAnalyticPath);

            Task<SentimentResponseObject> makeRequest = MakeRequest(comments.Take(5).ToArray());
            foreach (Document req in makeRequest.Result.documents)
            {
                Console.Out.WriteLine($"Nå skal vi sende {req.score}");
                await SendToAnalytic(publishClient, req.score.ToString());

            }

            await publishClient.CloseAsync();
        }

        static async Task<SentimentResponseObject> MakeRequest(RedditComment[] redditCommentBatch)
        {
            HttpClient client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bdd4428c00ab4b1095e904cb6b8a8ea2");

            // Request parameters
            string uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            //Send request
            List<SentimentQueryDocument> batch = redditCommentBatch
                .Select(rc => new SentimentQueryDocument {Id = rc.Id, Text = rc.Comment, Language = "en"})
                .ToList();

            SentimentQueryObject queryobject = new SentimentQueryObject { Documents = batch };
            StringContent content = new StringContent(JsonConvert.SerializeObject(queryobject));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            //Get answer
            HttpResponseMessage response = await client.PostAsync(uri, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();
            SentimentResponseObject resp = JsonConvert.DeserializeObject<SentimentResponseObject>(jsonResponse);
            return resp;
        }

        private static async Task SendToAnalytic(EventHubClient publishClient, string text)
        {
            // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
            // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
            // we are using the connection string from the namespace.


            //await SendMessagesToEventHub(100);
            await publishClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(text)));
        }

    }

    public class SentimentQueryObject
    {
        [JsonProperty("documents")]
        public IList<SentimentQueryDocument> Documents { get; set; }
    }

    public class SentimentQueryDocument
    {
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class SentimentResponseObject
    {
        public Document[] documents { get; set; }
        public object[] errors { get; set; }
    }

    public class Document
    {
        public float score { get; set; }
        public string id { get; set; }
    }

}