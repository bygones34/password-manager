using System;

namespace PasswordManager.Application.Abstractions;

/// <summary>
/// Port for resolving application data storage paths across execution environments.
/// </summary>
public interface IStoragePathProvider
{
    /// <summary>
    /// Gets the root directory path where application data and vaults are stored.
    /// </summary>
    string GetAppDataDirectory();

    /// <summary>
    /// Gets the absolute path for the main vault SQLite database file.
    /// </summary>
    string GetVaultDatabasePath();

    /// <summary>
    /// Gets the directory where encrypted backup snapshots are stored.
    /// </summary>
    string GetBackupsDirectory();

    /// <summary>
    /// Indicates whether the application is running inside a packaged (MSIX/AppX) container.
    /// </summary>
    bool IsPackagedEnvironment { get; }
}
