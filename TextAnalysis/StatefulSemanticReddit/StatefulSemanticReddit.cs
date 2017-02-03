using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using StatefulSemanticReddit.Analysis;
using StatefulSemanticReddit.Domain;
using StatefulSemanticReddit.EventInput;

namespace StatefulSemanticReddit
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class StatefulSemanticReddit : StatefulService
    {
        private const int BatchSize = 900;
        private const int PullDelay = 5;
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
            var eventHubClient = EventHubClient.CreateFromConnectionString(Config.EhConnectionString, Config.EhCommentPath);
            EventHubClient publishClient = EventHubClient.CreateFromConnectionString(Config.EhConnectionString, Config.EhAnalyticPath);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (ITransaction tx = StateManager.CreateTransaction())
                {

                 
                    StateHandler stateHandler = new StateHandler(tx, StateManager);

                    EventReader reader = new EventReader(eventHubClient, stateHandler);

                    IEnumerable<AnalysedRedditComment> comments = await reader.GetComments();
                        
                    IEnumerable<IReadOnlyDictionary<string, AnalysedRedditComment>> batches = BatchComments(comments, BatchSize);

                    foreach (IReadOnlyDictionary<string, AnalysedRedditComment> batch in batches)
                    {
                        LanguageAnalyzer languageAnalyzer = new LanguageAnalyzer();
                        SentimentAnalyzer sentimentAnalyzer = new SentimentAnalyzer();
                        KeyPhraseAnalyzer keyPhraseAnalyzer = new KeyPhraseAnalyzer();

                        await languageAnalyzer.DoAnalysis(batch); // do language analysis first
                        await sentimentAnalyzer.DoAnalysis(batch);
                        await keyPhraseAnalyzer.DoAnalysis(batch);

                        ProcessMessages(batch.Values, publishClient);
                    }

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(PullDelay), cancellationToken);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private IEnumerable<IReadOnlyDictionary<string, AnalysedRedditComment>> BatchComments(IEnumerable<AnalysedRedditComment> comments, int batchSize)
        {
            using (IEnumerator<AnalysedRedditComment> enumerator = comments.GetEnumerator())
            {
                int currentCount = 0;
                Dictionary<string, AnalysedRedditComment> commentDictionary = new Dictionary<string, AnalysedRedditComment>();

                while (enumerator.MoveNext())
                {
                    currentCount++;
                    commentDictionary.Add(enumerator.Current.Id, enumerator.Current);

                    if (currentCount < batchSize)
                        continue;

                    currentCount = 0;
                    yield return new ReadOnlyDictionary<string, AnalysedRedditComment>(commentDictionary);
                }

                if(currentCount > 0)
                    yield return new ReadOnlyDictionary<string, AnalysedRedditComment>(commentDictionary);
            }
        }

        public static async void ProcessMessages(IEnumerable<AnalysedRedditComment> comments, EventHubClient publishClient)
        {
            //EventHubClient publishClient = EventHubClient.CreateFromConnectionString(Config.EhConnectionString, Config.EhAnalyticPath);
            
            JsonSerializer serializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };

            foreach (AnalysedRedditComment req in comments)
            {
                StringBuilder sb = new StringBuilder();

                using (StringWriter sw = new StringWriter(sb))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, req);
                }

                string serializedContent = sb.ToString();
                await SendToAnalytic(publishClient, serializedContent);
            }
            
            //await publishClient.CloseAsync();
        }

        private static async Task SendToAnalytic(EventHubClient publishClient, string text)
        {
            await publishClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(text)));
        }

    }
}
