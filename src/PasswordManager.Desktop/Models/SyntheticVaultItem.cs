using System;

namespace PasswordManager.Desktop.Models;

/// <summary>
/// Synthetic credential item used for M0 UI shell verification.
/// Real encryption & vault storage is deferred to M1+.
/// </summary>
public record SyntheticVaultItem(
    Guid Id,
    string Title,
    string Username,
    string Password,
    string WebsiteUrl,
    string Category,
    bool IsFavorite,
    string Notes,
    DateTimeOffset LastModified)
{
    public string CategoryGlyph => Category switch
    {
        "Logins" => "\uE77B",    // Contact / Account
        "Servers" => "\uEDA2",   // Server
        "Notes" => "\uE70B",     // QuickNote
        _ => "\uE8D7"            // Key
    };

    public string MaskedPassword => new('•', Math.Min(Math.Max(Password.Length, 8), 16));

    public double FavoriteOpacity => IsFavorite ? 1.0 : 0.0;
}
