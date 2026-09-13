// <copyright file="CipherBankDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Configuration;

/// <summary>Loads repository-owned default configuration embedded in Core.</summary>
public static class CipherBankDefaultsConfiguration
{
    private const string WindowsResourceName = "CipherBank_app.Config.appsettings.Windows.json";

    private static readonly string[] RequiredResourceNames =
    [
        "CipherBank_app.Config.appsettings.json",
        "CipherBank_app.Config.network.endpoints.json",
    ];

    /// <summary>
    /// Maps compile-time host facts to the repository overlay order.
    /// Use: High (MAUI startup). Scope: process configuration.
    /// </summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot BuildForHost(bool isDevelopment, bool isWindows)
        => Build(isDevelopment ? "Development" : "Production", isWindows);

    /// <summary>
    /// Builds the default configuration, then optionally merges environment and Windows overlays.
    /// Use: High. Scope: host and test composition of embedded options.
    /// </summary>
    /// <param name="environment">Host environment name; unknown overlays are ignored.</param>
    /// <param name="windowsOverlay">Whether to merge Windows defaults after the environment.</param>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot Build(
        string? environment = null,
        bool windowsOverlay = false)
    {
        Assembly assembly = typeof(CipherBankDefaultsConfiguration).Assembly;
        ConfigurationBuilder builder = new ConfigurationBuilder();
        foreach (string resourceName in RequiredResourceNames)
        {
            builder.AddJsonStream(OpenRequiredResource(assembly, resourceName));
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            TryAddOptionalResource(
                builder,
                assembly,
                $"CipherBank_app.Config.appsettings.{environment}.json");
        }

        if (windowsOverlay)
        {
            TryAddOptionalResource(builder, assembly, WindowsResourceName);
        }

        return builder.Build();
    }

    private static Stream OpenRequiredResource(Assembly assembly, string resourceName)
        => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded configuration resource '{resourceName}'.");

    private static void TryAddOptionalResource(
        IConfigurationBuilder builder,
        Assembly assembly,
        string resourceName)
    {
        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            builder.AddJsonStream(stream);
        }
    }
}
