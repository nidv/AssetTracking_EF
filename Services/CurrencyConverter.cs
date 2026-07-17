using AssetTracking_EF.Models;

namespace AssetTracking_EF.Services;

// Level 3 requirement - currency conversion. Fixed USD-base exchange rates
public static class CurrencyConverter
{
    private static readonly Dictionary<Currency, decimal> _usdRate = new()
    {
        { Currency.USD, 1m },
        { Currency.EUR, 0.88m },
        { Currency.SEK, 9.68m },
        { Currency.TRY, 47m }
    };

    //Convert prices into local
    public static decimal ConvertFromUsd(decimal usd, Currency to) =>
        usd * _usdRate[to];

    //Display currency symbols
    public static string Symbol(Currency currency) => currency switch
    {
        Currency.USD => "$",
        Currency.EUR => "€",
        Currency.SEK => ";-",
        Currency.TRY => "₺",
        _ => currency.ToString()
    };
}
