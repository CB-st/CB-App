// <copyright file="IRecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite ACH recipients repo (Cora recipientsRepo).</summary>
public interface IRecipientRepository
{
    /// <summary>
    /// Ensures the persist schema exists before recipient reads or writes.
    /// Use: High (first payee list). Scope: IRecipientRepository consumers.
    /// </summary>
    Task EnsureSchemaAsync() => EnsureSchemaAsync(CancellationToken.None);

    Task EnsureSchemaAsync(CancellationToken ct);

    /// <summary>
    /// Lists stored payees as mask-only rows (no account or routing cleartext).
    /// Use: High (payee picker). Scope: IRecipientRepository consumers.
    /// </summary>
    Task<IReadOnlyList<AchRecipientRow>> ListAsync()
        => ListAsync(CancellationToken.None);

    Task<IReadOnlyList<AchRecipientRow>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Upserts payee metadata and masks. Cleartext account/routing inputs never enter the EF model.
    /// Use: High (payee save). Scope: IRecipientRepository consumers.
    /// </summary>
    Task UpsertAsync(AchRecipientRow row)
        => UpsertAsync(row, CancellationToken.None);

    Task UpsertAsync(AchRecipientRow row, CancellationToken ct);

    /// <summary>
    /// Deletes the payee with <paramref name="id"/> when it exists.
    /// Use: Medium (payee editor). Scope: IRecipientRepository consumers.
    /// </summary>
    Task DeleteAsync(string id)
        => DeleteAsync(id, CancellationToken.None);

    Task DeleteAsync(string id, CancellationToken ct);
}
