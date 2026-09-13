// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using Serilog;

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
        UserAppTheme = AppTheme.Dark;

        // MAUI has no async build hook (IMauiInitializeService is synchronous), so the
        // App constructor is the defined async startup path: start the initialization
        // task after the provider is built and surface failures through the log.
        // SendViewModel re-runs the idempotent initializer before recipient-list use.
        _ = SeedRecipientsAsync(recipientSeeds);
    }

    /// <summary>
    /// Creates the root window with a DI-aware AppShell.
    /// Use: High (once per process). Scope: application lifetime.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_services, _session, _db, _custody, _idleLock));
    }

    /// <summary>
    /// Seeds configured default recipients into a new database at startup.
    /// Failures are logged and never fatal; seeding is idempotent per configured ID.
    /// Use: Low (once per cold start). Scope: app startup.
    /// </summary>
    private static async Task SeedRecipientsAsync(IRecipientSeedInitializer recipientSeeds)
    {
        try
        {
            await recipientSeeds.InitializeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Recipient seed initialization failed");
        }
    }
}
