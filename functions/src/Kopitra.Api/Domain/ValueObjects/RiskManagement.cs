using EventFlow.ValueObjects;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Stop loss and take profit configuration
/// </summary>
public class RiskManagement : ValueObject
{
    public decimal? StopLoss { get; private set; }
    public decimal? TakeProfit { get; private set; }

    private RiskManagement()
    {
    }

    public RiskManagement(decimal? stopLoss = null, decimal? takeProfit = null)
    {
        if (stopLoss.HasValue && stopLoss.Value <= 0)
            throw new ArgumentException("Stop loss must be greater than zero.", nameof(stopLoss));
        if (takeProfit.HasValue && takeProfit.Value <= 0)
            throw new ArgumentException("Take profit must be greater than zero.", nameof(takeProfit));

        StopLoss = stopLoss;
        TakeProfit = takeProfit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StopLoss;
        yield return TakeProfit;
    }
}
