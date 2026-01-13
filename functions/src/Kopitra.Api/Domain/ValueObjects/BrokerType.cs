using System.ComponentModel.DataAnnotations;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Broker type enumeration (MT4 or MT5)
/// </summary>
public enum BrokerType
{
    [Display(Name = "MetaTrader 4")]
    MT4 = 1,

    [Display(Name = "MetaTrader 5")]
    MT5 = 2,
}
