﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using SemanticRedditCore.Domain;
using SemanticRedditCore.EventInput;

namespace SemanticRedditCore
{
    static class Program
    {
        private static string OcpApimSubscriptionKey;
        private static string EhConnectionString;
        private static string EhAnalyticPath = "analytics";
        private static string EhCommentPath = "reddit-comments";
        private static string StorageContainerName;
        private static string StorageAccountName;
        private static string StorageAccountKey;
        private static string StorageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        private static string OffsetFileName;

        public static void Main()
        {
            OcpApimSubscriptionKey = CloudConfigurationManager.GetSetting("OcpApimSubscriptionKey");
            EhConnectionString = CloudConfigurationManager.GetSetting(nameof(EhConnectionString));
            StorageContainerName = CloudConfigurationManager.GetSetting(nameof(StorageContainerName));
            StorageAccountName = CloudConfigurationManager.GetSetting(nameof(StorageAccountName));
            StorageAccountKey = CloudConfigurationManager.GetSetting(nameof(StorageAccountKey));
            OffsetFileName = CloudConfigurationManager.GetSetting(nameof(OffsetFileName));

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
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", OcpApimSubscriptionKey);

            // Request parameters
            string uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            //Send request
            List<SentimentQueryDocument> batch = redditCommentBatch
                .Select(rc => new SentimentQueryDocument { Id = rc.Id, Text = rc.Comment, Language = "en" })
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
}