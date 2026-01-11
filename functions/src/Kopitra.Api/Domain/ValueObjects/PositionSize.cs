using EventFlow.ValueObjects;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Position size with multiple sizing strategies
/// </summary>
public class PositionSize : ValueObject
{
    public decimal? FixedLot { get; private set; }
    public decimal? PercentageOfBalance { get; private set; }
    public bool IsProportional { get; private set; }

    private PositionSize()
    {
    }

    public PositionSize(decimal fixedLot)
    {
        if (fixedLot <= 0)
            throw new ArgumentException("Fixed lot must be greater than zero.", nameof(fixedLot));
        FixedLot = fixedLot;
        IsProportional = false;
    }

    public PositionSize(decimal percentage, bool isPercentage)
    {
        if (!isPercentage)
            throw new ArgumentException("Use the decimal constructor for fixed lot.", nameof(isPercentage));
        if (percentage <= 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 0 and 100.", nameof(percentage));
        PercentageOfBalance = percentage;
        IsProportional = false;
    }

    public static PositionSize CreateProportional() => new() { IsProportional = true };

    public static PositionSize CreateFixedLot(decimal lot) => new(lot);

    public static PositionSize CreatePercentage(decimal percentage) => new(percentage, true);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FixedLot;
        yield return PercentageOfBalance;
        yield return IsProportional;
    }
}
