using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using SemanticRedditCore.Domain;

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

        public async Task<IEnumerable<RedditComment>> GetComments(string consumerGroupName, string partitionId)
        {
            string textFromFile = await _stateHandler.GetOffset();

            EventHubReceiver eventReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver("1", textFromFile);

            EventData eventData = eventReceiver.Receive();

            await _stateHandler.SetOffset(eventData.Offset);

            return DeserializeObject(eventData).Where(c => c != null);
        }

        private static List<RedditComment> DeserializeObject(EventData d)
        {
            try
            {
                byte[] bytes = d.GetBytes();
                string text = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<List<RedditComment>>(text);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
