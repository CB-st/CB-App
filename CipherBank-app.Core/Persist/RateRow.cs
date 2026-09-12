// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>A cached market rate. <see cref="Symbol"/> normalizes to uppercase at construction.</summary>
public sealed record RateRow(string Symbol, decimal Usd, decimal Change24h, long UpdatedAtMs)
{
    private readonly string _symbol = Normalize(Symbol);

    /// <summary>Gets the asset symbol, always uppercase invariant.</summary>
    public string Symbol
    {
        get => _symbol;
        init => _symbol = Normalize(value);
    }

    /// <summary>Maps a one-unit inverse quote to its persisted USD rate.</summary>
    public static RateRow FromQuote(PublicQuote quote, long updatedAtMs)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new RateRow(
            quote.InputCurrency,
            quote.Rate,
            Change24h: 0m,
            updatedAtMs);
    }

    private static string Normalize(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return symbol.ToUpperInvariant();
    }
}
