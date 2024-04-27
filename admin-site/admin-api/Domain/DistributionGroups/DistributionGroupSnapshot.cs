using EventFlow.Snapshots;

namespace AdminApi.Domain.DistributionGroups
{
    [SnapshotVersion("DistributionGroup", 1)]
    public class DistributionGroupSnapshot : ISnapshot
    {
        public DistributionGroupSnapshot(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}