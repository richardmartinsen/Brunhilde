using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using StatefulSemanticReddit.Domain;

namespace StatefulSemanticReddit.EventInput
{
    public sealed class EventReader
    {
        private readonly StateHandler _stateHandler;
        private readonly EventHubClient _eventHubClient;

        //public EventReader(string connectionString, string commentPath, StateHandler stateHandler)
        //{
        //    _stateHandler = stateHandler;
        //    _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, commentPath);
        //}

        public EventReader(EventHubClient eventHubClient, StateHandler stateHandler)
        {
            _eventHubClient = eventHubClient;
            _stateHandler = stateHandler;

        }

        public async Task<IEnumerable<AnalysedRedditComment>> GetComments()
        {
            IEnumerable<EventData> data = await GetEventData();

            return DeserializeObjects(data)
                .Where(c => c != null);
        }

        private async Task<IEnumerable<EventData>> GetEventData()
        {
            EventHubRuntimeInformation runtimeInformation = _eventHubClient.GetRuntimeInformation();
            List<EventData> dataRecieved = new List<EventData>();

            foreach (string partitionId in runtimeInformation.PartitionIds)
            {
                string partitionOffset = await _stateHandler.GetOffset(partitionId);

                //EventHubReceiver eventReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partitionId, partitionOffset);
                EventHubReceiver eventReceiver = _eventHubClient.GetConsumerGroup("mojo").CreateReceiver(partitionId, partitionOffset);

                EventData eventData = eventReceiver.Receive();

                await _stateHandler.SetOffset(eventData.Offset, partitionId);
                await eventReceiver.CloseAsync();
                dataRecieved.Add(eventData);
                
            }

            return dataRecieved;
        }

        private static IEnumerable<AnalysedRedditComment> DeserializeObjects(IEnumerable<EventData> data)
        {
            var redditComments = new List<AnalysedRedditComment>();
            foreach (EventData d in data)
            {
                try
                {
                    byte[] bytes = d.GetBytes();
                    string text = Encoding.UTF8.GetString(bytes);
                    
                    var analysedRedditComments = JsonConvert.DeserializeObject<List<RedditComment>>(text).Select(rc => new AnalysedRedditComment { Id = rc.Id, Comment = rc.Comment, CreatedUTC = rc.CreatedUTC, UpVotes = rc.UpWotes, DownVotes = rc.DownVotes, SubReddit = rc.SubReddit});
                    redditComments.AddRange(analysedRedditComments);
                }
                catch (Exception e)
                {
                    return Enumerable.Empty<AnalysedRedditComment>();
                }
            }

            return redditComments;
        }
    }
}
