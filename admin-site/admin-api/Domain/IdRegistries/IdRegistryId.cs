using EventFlow.Core;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryId : Identity<IdRegistryId>
    {
        public IdRegistryId(string value) : base(value) { }
    }
}
