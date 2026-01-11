using EventFlow.ValueObjects;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Trading symbol value object (e.g., "EURUSD")
/// </summary>
public class TradingSymbol : SingleValueObject<string>
{
    public TradingSymbol(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Trading symbol cannot be empty.", nameof(value));
        if (value.Length > 10)
            throw new ArgumentException("Trading symbol must be 10 characters or less.", nameof(value));
    }

    public static TradingSymbol Create(string value) => new(value);
}
