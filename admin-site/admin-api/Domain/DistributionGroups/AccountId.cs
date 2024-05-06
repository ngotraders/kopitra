using EventFlow.Core;

namespace AdminApi.Domain.DistributionGroups
{
    public class AccountId : Identity<AccountId>
    {
        public AccountId(string value) : base(value) { }
    }
}
