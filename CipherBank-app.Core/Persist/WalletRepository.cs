// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class WalletRepository : IWalletRepository
{
    private readonly ILocalDb _db;

    public WalletRepository(ILocalDb db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LocalWalletRow>> ListAsync(CancellationToken ct = default)
    {
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            return await context.Wallets
                .AsNoTracking()
                .OrderBy(entity => entity.CreatedAt)
                .Select(entity => new LocalWalletRow(
                    entity.Id,
                    entity.Symbol,
                    entity.Label,
                    entity.Address,
                    entity.Path,
                    entity.AccountIndex,
                    entity.Kind,
                    entity.CreatedAt))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }

    public Task UpsertAsync(LocalWalletRow row, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(row);
        return UpsertCoreAsync(row, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            WalletEntity? entity = await context.Wallets.FindAsync([id], ct).ConfigureAwait(false);
            if (entity is null)
            {
                return;
            }

            context.Wallets.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task UpsertCoreAsync(LocalWalletRow row, CancellationToken ct)
    {
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            WalletEntity? entity = await context.Wallets.FindAsync([row.Id], ct).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new WalletEntity { Id = row.Id, CreatedAt = row.CreatedAt };
                context.Wallets.Add(entity);
            }

            entity.Symbol = row.Symbol;
            entity.Label = row.Label;
            entity.Address = row.Address;
            entity.Path = row.Path;
            entity.AccountIndex = row.AccountIndex;
            entity.Kind = row.Kind;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
