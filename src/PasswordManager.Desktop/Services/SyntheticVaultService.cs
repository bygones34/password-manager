using System;
using System.Collections.Generic;
using PasswordManager.Desktop.Models;

namespace PasswordManager.Desktop.Services;

/// <summary>
/// Provides synthetic, test-only credentials for UI shell validation in M0.
/// </summary>
public static class SyntheticVaultService
{
    public static IReadOnlyList<SyntheticVaultItem> GetSampleItems() =>
    [
        new SyntheticVaultItem(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title: "GitHub (Synthetic Test)",
            Username: "test.dev@example.local",
            Password: "synthetic-github-pass-1234!",
            WebsiteUrl: "https://github.com",
            Category: "Logins",
            IsFavorite: true,
            Notes: "Synthetic test item for personal developer credentials.",
            LastModified: DateTimeOffset.UtcNow.AddDays(-2)),

        new SyntheticVaultItem(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Title: "ProtonMail (Synthetic Test)",
            Username: "user.sample@proton.local",
            Password: "synthetic-proton-pass-5678#",
            WebsiteUrl: "https://mail.proton.me",
            Category: "Logins",
            IsFavorite: true,
            Notes: "Primary mock mailbox entry.",
            LastModified: DateTimeOffset.UtcNow.AddHours(-14)),

        new SyntheticVaultItem(
            Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Title: "AWS Management Console (Synthetic)",
            Username: "aws.admin@example.local",
            Password: "synthetic-aws-pass-9012$",
            WebsiteUrl: "https://aws.amazon.com/console",
            Category: "Logins",
            IsFavorite: false,
            Notes: "Staging cloud tenant mock access.",
            LastModified: DateTimeOffset.UtcNow.AddDays(-7)),

        new SyntheticVaultItem(
            Id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Title: "Internal NAS Server (Synthetic)",
            Username: "nas_admin",
            Password: "synthetic-nas-pass-3456%",
            WebsiteUrl: "https://nas.home.local",
            Category: "Servers",
            IsFavorite: false,
            Notes: "Local network backup server storage credentials.",
            LastModified: DateTimeOffset.UtcNow.AddDays(-12)),

        new SyntheticVaultItem(
            Id: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Title: "Home Wi-Fi WPA3 Key (Synthetic)",
            Username: "SSID: LabNetwork_5G",
            Password: "synthetic-wifi-pass-7890^",
            WebsiteUrl: "https://router.local",
            Category: "Notes",
            IsFavorite: true,
            Notes: "WPA3 Personal lab network pre-shared key.",
            LastModified: DateTimeOffset.UtcNow.AddDays(-30))
    ];
}
