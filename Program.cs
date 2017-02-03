using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Azure.EventHubs;

namespace CSHttpClientSample
{
    static class Program
    {
        private static EventHubClient eventHubClient;
        private const string EhConnectionString = "Endpoint=sb://dev-brunhilde.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=jfHBVw0tKFzbyhlgQjV25RQRfRfCPOiy+/1zPhEuYiI=";
        private const string EhAnalyticPath = "analytics";
        private const string EhCommentPath = "reddit-comments";
        private const string StorageContainerName = "{Storage account container name}";
        private const string StorageAccountName = "{Storage account name}";
        private const string StorageAccountKey = "{Storage account key}";
        private static readonly string StorageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);

        public static void Main()
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EhConnectionString)
            {
                EntityPath = EhAnalyticPath
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            ProcessMessages();
            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }


        public static async void ProcessMessages()
        {
            Task<SentimentResponseObject> makeRequest = MakeRequest();
            foreach (var req in makeRequest.Result.documents)
            {
                Console.Out.WriteLine("Nå skal vi sende " + req.score.ToString());
                await SendToAnalytic(req.score.ToString());
            }
        }

        static async Task<SentimentResponseObject> MakeRequest()
        {
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bdd4428c00ab4b1095e904cb6b8a8ea2");

            // Request parameters
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            //Send request
            var doc = new SentimentQueryDocument { Language = "en", Id = "1234", Text = "this is super happy" };
            var doc2 = new SentimentQueryDocument { Language = "en", Id = "2345", Text = "this is crap" };
            var queryobject = new SentimentQueryObject { Documents = new List<SentimentQueryDocument> { doc, doc2 } };
            var content = new StringContent(JsonConvert.SerializeObject(queryobject));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            //Get answer
            var response = await client.PostAsync(uri, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<SentimentResponseObject>(jsonResponse);
            return resp;
        }

        private static async Task SendToAnalytic(string text)
        {
            // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
            // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
            // we are using the connection string from the namespace.


            //await SendMessagesToEventHub(100);
            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(text)));

            await eventHubClient.CloseAsync();
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