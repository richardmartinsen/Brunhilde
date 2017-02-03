//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Azure.EventHubs;
//using Microsoft.Azure.EventHubs.Processor;

//namespace Brunhilde
//{
//    //Endpoint=sb://dev-brunhilde.servicebus.windows.net/;SharedAccessKeyName=reddit;SharedAccessKey=de0fz35s+6BQ0HmYmqf4lUI8R+EsmKxZrLkCaA2O7E4=

//    public class SimpleEventProcessor : IEventProcessor
//    {
//        public Task CloseAsync(PartitionContext context, CloseReason reason)
//        {
//            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
//            return Task.CompletedTask;
//        }

//        public Task OpenAsync(PartitionContext context)
//        {
//            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
//            return Task.CompletedTask;
//        }

//        public Task ProcessErrorAsync(PartitionContext context, Exception error)
//        {
//            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
//            return Task.CompletedTask;
//        }

//        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
//        {
//            foreach (EventData eventData in messages)
//            {
//                string data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
//                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
//            }

//            return context.CheckpointAsync();
//        }

//        public Task ProcessEventsAsync(PartitionContext context, IEnumerable messages)
//        {
//            IEnumerable<EventData> msgs = messages.Cast<EventData>();

//            return ProcessEventsAsync(context, msgs);
//        }
//    }
//}
