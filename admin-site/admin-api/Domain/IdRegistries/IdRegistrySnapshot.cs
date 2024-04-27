using EventFlow.Snapshots;

namespace AdminApi.Domain.IdRegistries
{
    [SnapshotVersion("IdRegistry", 1)]
    public class IdRegistrySnapshot : ISnapshot
    {
        public IReadOnlyCollection<KeyValuePair<string, string>> KeyIdPairs { private set; get; }

        public IdRegistrySnapshot(IReadOnlyCollection<KeyValuePair<string, string>> keyIdPairs)
        {
            KeyIdPairs = keyIdPairs.ToList();
        }
    }
}