using System;
using System.IO;
using PasswordManager.Platform.Windows.Storage;
using Xunit;

namespace PasswordManager.Windows.Tests;

public class WindowsStoragePathResolverTests : IDisposable
{
    private readonly string _tempTestDir;

    public WindowsStoragePathResolverTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), "PM_StorageTest_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void StoragePathResolver_DefaultPaths_ShouldResolveExpectedSubPaths()
    {
        var resolver = new WindowsStoragePathResolver(_tempTestDir);

        var appData = resolver.GetAppDataDirectory();
        var dbPath = resolver.GetVaultDatabasePath();
        var backupsPath = resolver.GetBackupsDirectory();

        Assert.Equal(_tempTestDir, appData);
        Assert.Equal(Path.Combine(_tempTestDir, "vault.db"), dbPath);
        Assert.Equal(Path.Combine(_tempTestDir, "backups"), backupsPath);
        Assert.True(Directory.Exists(appData));
        Assert.True(Directory.Exists(backupsPath));
    }

    [Fact]
    public void EnsureDirectoryWithRestrictedAcl_ShouldCreateDirectorySafely()
    {
        var testPath = Path.Combine(_tempTestDir, "RestrictedSubFolder");
        Assert.False(Directory.Exists(testPath));

        WindowsStoragePathResolver.EnsureDirectoryWithRestrictedAcl(testPath);

        Assert.True(Directory.Exists(testPath));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup in test temp folder
        }
    }
}
