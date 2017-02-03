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

        public async Task<string> GetOffset()
        {
            IReliableDictionary<string, string> myDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("offsetDictionary");

            ConditionalValue<string> result = await myDictionary.TryGetValueAsync(_transaction, "OffsetCounter");

            return result.HasValue ? result.Value : string.Empty;
        }

        public async Task SetOffset(string offset)
        {
            IReliableDictionary<string, string> myDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("offsetDictionary");

            await myDictionary.AddOrUpdateAsync(_transaction, "OffsetCounter", offset, (key, value) => offset);
        }
    }
}
