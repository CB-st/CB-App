// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;

namespace CipherBank_app;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly IAppSession _session;
    private readonly ILocalDb _db;
    private readonly ICustodyService _custody;
    private readonly AppIdleLockService _idleLock;
    private readonly IRecipientSeedInitializer _recipientSeeds;

    public App(
        IServiceProvider services,
        IAppSession session,
        ILocalDb db,
        ICustodyService custody,
        AppIdleLockService idleLock,
        IRecipientSeedInitializer recipientSeeds)
    {
        InitializeComponent();
        _services = services;
        _session = session;
        _db = db;
        _custody = custody;
        _idleLock = idleLock;
        _recipientSeeds = recipientSeeds;
        UserAppTheme = AppTheme.Dark;
    }

    /// <summary>
    /// Creates the root window with a DI-aware AppShell. Recipient seeding is awaited
    /// inside AppShell's gated splash boot, so the shell cannot expose a recipient
    /// surface before seeding commits.
    /// Use: High (once per process). Scope: application lifetime.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_services, _session, _db, _custody, _idleLock, _recipientSeeds));
    }
}
