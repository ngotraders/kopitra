using EventFlow.Core;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupId : Identity<DistributionGroupId>
    {
        public DistributionGroupId(string value) : base(value) { }
    }
}
