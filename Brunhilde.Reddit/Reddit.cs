using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using RedditSharp;

namespace Brunhilde.Reddit
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class DataScraper : StatefulService
    {
        static string eventHubName = "reddit-comments";
        static string connectionString = "Endpoint=sb://dev-brunhilde.servicebus.windows.net/;SharedAccessKeyName=reddit;SharedAccessKey=de0fz35s+6BQ0HmYmqf4lUI8R+EsmKxZrLkCaA2O7E4=";

        public DataScraper(StatefulServiceContext context)
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
            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
            //var eventHubSender = EventHubSender.CreateFromConnectionString(connectionString);

            var redditCounter = await StateManager.GetOrAddAsync<IReliableDictionary<string, DateTime>>("reddit");

            var reddit = new RedditSharp.Reddit(WebAgent.RateLimitMode.Pace);
            var user = reddit.LogIn("leap17", "leap17");
            var subreddit = reddit.GetSubreddit("/r/pics/");

            var lastDateTime = DateTime.UtcNow;

            var comments = new List<CommentShape>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var tx = StateManager.CreateTransaction())
                {
                    var result = await redditCounter.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(Context, "Current lastTime Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    try
                    {
                        var subComments =
                        subreddit.Comments.Take(10).OrderBy(c => c.CreatedUTC).Where(c => c.CreatedUTC > lastDateTime);

                        foreach (var comment in subComments)
                        {
                            lastDateTime = comment.CreatedUTC;
                            await redditCounter.AddOrUpdateAsync(tx, "Counter", DateTime.UtcNow,
                                (key, value) => lastDateTime);
                            var commentLinkId = comment.LinkId;

                            comments.Add(new CommentShape
                            {
                                Comment = comment.Body,
                                UpWotes = comment.Upvotes,
                                DownVotes = comment.Downvotes,
                                Id = comment.Id,
                                CreatedUTC = comment.CreatedUTC,
                                Author = comment.Author
                            }
                            );
                        }
                        if (comments.Any())
                        {
                            //Send Comment To EventHub
                            var message = JsonConvert.SerializeObject(comments);
                            //await eventHubSender.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
                            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
                        }
                        ServiceEventSource.Current.ServiceMessage(Context, "Got {0} new comments",
                            comments.Count);
                        comments.Clear();
                    }
                    catch (Exception e)
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, "Error happend: {0}",
                            e.Message);
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }

                    await tx.CommitAsync();
                }
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    internal class CommentShape
    {
        public string Comment { get; set; }
        public int UpWotes { get; set; }
        public int DownVotes { get; set; }
        public string Id { get; set; }
        public DateTime CreatedUTC { get; set; }
        public string Author { get; set; }
    }
}
