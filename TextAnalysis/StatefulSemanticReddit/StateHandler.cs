using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace StatefulSemanticReddit
{
    public sealed class StateHandler
    {
        private readonly ITransaction _transaction;
        private readonly IReliableStateManager _stateManager;

        public StateHandler(ITransaction transaction, IReliableStateManager stateManager)
        {
            _transaction = transaction;
            _stateManager = stateManager;
        }

        public async Task<string> GetOffset(string partitionId)
        {
            IReliableDictionary<string, string> myDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("offsetDictionary");

            ConditionalValue<string> result = await myDictionary.TryGetValueAsync(_transaction, GetPartitionOffsetKey(partitionId));

            return result.HasValue ? result.Value : string.Empty;
        }

        public async Task SetOffset(string offset, string partitionId)
        {
            IReliableDictionary<string, string> myDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("offsetDictionary");

            await myDictionary.AddOrUpdateAsync(_transaction, GetPartitionOffsetKey(partitionId), offset, (key, value) => offset);
        }

        private static string GetPartitionOffsetKey(string partitionId)
        {
            return $"{partitionId}|PartitionOffset";
        }
    }
}
