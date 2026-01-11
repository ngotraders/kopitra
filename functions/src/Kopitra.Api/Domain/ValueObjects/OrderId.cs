using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Order aggregate ID
/// </summary>
public class OrderId : Identity<OrderId>
{
    public OrderId(string value) : base(value)
    {
    }
}
