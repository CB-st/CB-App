// <copyright file="PersistenceOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Settings for the on-device EF Core database.</summary>
public sealed class PersistenceOptions
{
    public static string SectionName { get; } = "Persistence";

    public string DatabaseName { get; set; } = "cipherbank.db";

    /// <summary>
    /// Demo payees inserted when the recipients table is empty. Stable ids; changing them duplicates rows.
    /// </summary>
    public IList<DefaultRecipientOptions> DefaultRecipients { get; } = new List<DefaultRecipientOptions>();

    /// <summary>
    /// Validates the database filename and every configured bootstrap recipient.
    /// Use: High (startup options validation). Scope: persistence composition.
    /// </summary>
    public bool IsValid()
        => IsDatabaseNameValid() && AreDefaultRecipientsValid();

    /// <summary>
    /// True when every seed row has a unique non-blank id and name. An empty list is valid (no seed).
    /// Use: Medium (options bind / repository construction). Scope: PersistenceOptions.
    /// </summary>
    public bool AreDefaultRecipientsValid()
    {
        List<string> ids = new List<string>(DefaultRecipients.Count);
        List<string> names = new List<string>(DefaultRecipients.Count);
        foreach (DefaultRecipientOptions row in DefaultRecipients)
        {
            if (!IsRecipientValid(row))
            {
                return false;
            }

            if (ids.Exists(id => string.Equals(id, row.Id, StringComparison.Ordinal))
                || names.Exists(name => string.Equals(name, row.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            ids.Add(row.Id);
            names.Add(row.Name);
        }

        return true;
    }

    private static bool IsRecipientValid(DefaultRecipientOptions row)
        => !string.IsNullOrWhiteSpace(row.Id)
            && !string.IsNullOrWhiteSpace(row.Name)
            && !string.IsNullOrWhiteSpace(row.Holder)
            && !string.IsNullOrWhiteSpace(row.Bank)
            && IsRoutingValid(row.Routing)
            && !string.IsNullOrWhiteSpace(row.Account)
            && row.Account.Trim().Length >= 4
            && (string.Equals(row.AccountType, "checking", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.AccountType, "savings", StringComparison.OrdinalIgnoreCase))
            && (row.Memo is null || row.Memo.Length <= 140);

    private static bool IsRoutingValid(string? routing)
        => routing is not null
            && routing.Length == 9
            && routing.All(static character => character is >= '0' and <= '9');

    private bool IsDatabaseNameValid()
        => !string.IsNullOrWhiteSpace(DatabaseName)
            && !Path.IsPathRooted(DatabaseName)
            && string.Equals(Path.GetFileName(DatabaseName), DatabaseName, StringComparison.Ordinal);
}
