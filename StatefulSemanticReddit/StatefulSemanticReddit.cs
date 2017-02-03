using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using SemanticRedditCore;
using SemanticRedditCore.Domain;
using StatefulSemanticReddit.EventInput;

namespace StatefulSemanticReddit
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class StatefulSemanticReddit : StatefulService
    {
        public StatefulSemanticReddit(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IReliableDictionary<string, string> myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("offsetDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    ConditionalValue<string> result = await myDictionary.TryGetValueAsync(tx, "OffsetCounter");

                    ServiceEventSource.Current.ServiceMessage(Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value : "Value does not exist.");

                    StateHandler stateHandler = new StateHandler(tx, StateManager);

                    EventReader reader = new EventReader(Config.EhConnectionString, Config.EhCommentPath, stateHandler);

                    IEnumerable<RedditComment> comments = await reader.GetComments("$Default", "1");

                    ProcessMessages(comments);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public static async void ProcessMessages(IEnumerable<RedditComment> comments)
        {
            EventHubClient publishClient = EventHubClient.CreateFromConnectionString(Config.EhConnectionString, Config.EhAnalyticPath);

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
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Config.OcpApimSubscriptionKey);

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
