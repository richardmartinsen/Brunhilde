using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CSHttpClientSample
{
    static class Program
    {
        static void Main()
        {
            MakeRequest();
            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }

        static async void MakeRequest()
        {
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bdd4428c00ab4b1095e904cb6b8a8ea2");

            // Request parameters
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            //Send request
            var doc = new SentimentQueryDocument { Language = "en", Id = "kfsf", Text = "this is super happy" };
            var queryobject = new SentimentQueryObject { Documents = new List<SentimentQueryDocument> { doc } };
            var content = new StringContent(JsonConvert.SerializeObject(queryobject));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            //Get answer
            var response = await client.PostAsync(uri, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<SentimentResponseObject>(jsonResponse);

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