using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs.Processor;

namespace Brunhilde.EventInput
{
    public sealed class EventReader
    {
        private IEventProcessor _eventProcessor;
        private PartitionContext _partitionContext;

        public EventReader(PartitionContext partitionContext)
        {
            _eventProcessor = new SimpleEventProcessor();
        }
    }
}
