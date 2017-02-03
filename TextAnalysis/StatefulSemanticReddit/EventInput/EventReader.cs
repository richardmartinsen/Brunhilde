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

        public EventReader(string connectionString, string commentPath, StateHandler stateHandler)
        {
            _stateHandler = stateHandler;
            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, commentPath);
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

                EventHubReceiver eventReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partitionId, partitionOffset);

                EventData eventData = eventReceiver.Receive();

                await _stateHandler.SetOffset(eventData.Offset, partitionId);

                dataRecieved.Add(eventData);
            }

            return dataRecieved;
        }

        private static IEnumerable<AnalysedRedditComment> DeserializeObjects(IEnumerable<EventData> data)
        {
            foreach (EventData d in data)
            {
                try
                {
                    byte[] bytes = d.GetBytes();
                    string text = Encoding.UTF8.GetString(bytes);
                    return JsonConvert.DeserializeObject<List<RedditComment>>(text)
                        .Select(rc => new AnalysedRedditComment { Id = rc.Id, Comment = rc.Comment, CreatedUTC = rc.CreatedUTC, UpVotes = rc.UpWotes, DownVotes = rc.DownVotes });
                }
                catch (Exception e)
                {
                    return Enumerable.Empty<AnalysedRedditComment>();
                }
            }

            return Enumerable.Empty<AnalysedRedditComment>();
        }
    }
}
