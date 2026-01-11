namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Funding strategy for copy trading
/// </summary>
public enum FundingStrategy
{
    FixedLot = 0,
    Proportional = 1,
    Percentage = 2
}
