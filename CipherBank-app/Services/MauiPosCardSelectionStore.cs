// <copyright file="MauiPosCardSelectionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed class MauiPosCardSelectionStore : IPosCardSelectionStore
{
    private const string ActiveCardKey = "pos_active_card";

    public string Get(string fallback) => Preferences.Default.Get(ActiveCardKey, fallback);

    public void Set(string cardId) => Preferences.Default.Set(ActiveCardKey, cardId);
}
