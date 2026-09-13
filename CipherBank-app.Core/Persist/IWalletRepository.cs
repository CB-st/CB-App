// <copyright file="IWalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite wallets repo.</summary>
public interface IWalletRepository
{
    /// <summary>
    /// Lists on-device wallet rows (address/path metadata, not key material).
    /// Use: High (home / receive). Scope: IWalletRepository consumers.
    /// </summary>
    Task<IReadOnlyList<LocalWalletRow>> ListAsync()
        => ListAsync(CancellationToken.None);

    Task<IReadOnlyList<LocalWalletRow>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts or replaces a wallet row by id.
    /// Use: High (wallet create). Scope: IWalletRepository consumers.
    /// </summary>
    Task UpsertAsync(LocalWalletRow row)
        => UpsertAsync(row, CancellationToken.None);

    Task UpsertAsync(LocalWalletRow row, CancellationToken ct);

    /// <summary>
    /// Deletes the wallet with <paramref name="id"/> when it exists.
    /// Use: Medium (wallet editor). Scope: IWalletRepository consumers.
    /// </summary>
    Task DeleteAsync(string id)
        => DeleteAsync(id, CancellationToken.None);

    Task DeleteAsync(string id, CancellationToken ct);
}
