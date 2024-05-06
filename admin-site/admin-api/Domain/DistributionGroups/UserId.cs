using EventFlow.Core;

namespace AdminApi.Domain.DistributionGroups
{
    public class UserId : Identity<UserId>
    {
        public UserId(string value) : base(value) { }
    }
}
