using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Brunhilde.Domain;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Brunhilde.EventInput
{
    public sealed class EventReader
    {
        private readonly EventHubClient _eventHubClient;
        private string _offsetFileName;

        public EventReader(string connectionString, string commentPath, string allText)
        {
            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, commentPath);
            _offsetFileName = allText;
        }

        public IEnumerable<RedditComment> GetComments(string consumerGroupName, string partitionId, DateTime startTime)
        {
            string textFromFile = string.Empty;

            try
            {
                textFromFile = File.ReadAllText(_offsetFileName);
            }
            catch (Exception e)
            {
            }

            EventHubReceiver eventReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver("1", textFromFile);

            while (1 == 1)
            {
                EventData eventData = eventReceiver.Receive();

                File.WriteAllText(_offsetFileName, eventData.Offset);


                foreach (RedditComment redditComment in DeserializeObject(eventData))
                {
                    if(redditComment != null)
                        yield return redditComment;
                }
            }
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
