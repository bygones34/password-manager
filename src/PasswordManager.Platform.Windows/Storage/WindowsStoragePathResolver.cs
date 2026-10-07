using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using PasswordManager.Application.Abstractions;

namespace PasswordManager.Platform.Windows.Storage;

/// <summary>
/// Windows-specific storage path resolver and secure directory ACL manager.
/// Differentiates between packaged (MSIX) and unpackaged (%LocalAppData%) data roots.
/// </summary>
public sealed class WindowsStoragePathResolver : IStoragePathProvider
{
    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;
    private const string AppFolderName = "PasswordManager";
    private const string VaultFileName = "vault.db";
    private const string BackupsFolderName = "backups";

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder? packageFullName);

    public bool IsPackagedEnvironment { get; }

    private readonly string _resolvedRootDirectory;

    public WindowsStoragePathResolver(string? customRootOverride = null)
    {
        IsPackagedEnvironment = CheckIsPackaged();

        if (!string.IsNullOrEmpty(customRootOverride))
        {
            _resolvedRootDirectory = customRootOverride;
        }
        else if (IsPackagedEnvironment)
        {
            // For MSIX packaged app, resolve Windows.Storage.ApplicationData or LocalAppData Packages
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _resolvedRootDirectory = Path.Combine(localAppData, AppFolderName);
        }
        else
        {
            // Unpackaged standard Win32 location: %LOCALAPPDATA%\PasswordManager
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _resolvedRootDirectory = Path.Combine(localAppData, AppFolderName);
        }
    }

    public string GetAppDataDirectory()
    {
        EnsureDirectoryWithRestrictedAcl(_resolvedRootDirectory);
        return _resolvedRootDirectory;
    }

    public string GetVaultDatabasePath()
    {
        var root = GetAppDataDirectory();
        return Path.Combine(root, VaultFileName);
    }

    public string GetBackupsDirectory()
    {
        var root = GetAppDataDirectory();
        var backupsPath = Path.Combine(root, BackupsFolderName);
        EnsureDirectoryWithRestrictedAcl(backupsPath);
        return backupsPath;
    }

    /// <summary>
    /// Creates the directory if it does not exist and enforces restricted Windows NTFS ACLs,
    /// ensuring only the current user and local administrators have access permissions.
    /// </summary>
    public static void EnsureDirectoryWithRestrictedAcl(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath)) return;

        var dirInfo = new DirectoryInfo(directoryPath);
        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }

        try
        {
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser is null) return;

            var dSecurity = new DirectorySecurity();

            // Disable inheritance and remove existing inherited rules
            dSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Grant FullControl strictly to Current User
            dSecurity.AddAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            // Grant FullControl to local Administrators group
            var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            dSecurity.AddAccessRule(new FileSystemAccessRule(
                adminsSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            // Grant FullControl to Local System
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            dSecurity.AddAccessRule(new FileSystemAccessRule(
                systemSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(dSecurity);
        }
        catch (PlatformNotSupportedException)
        {
            // Non-Windows or unsupported file system (e.g. FAT32)
        }
        catch (UnauthorizedAccessException)
        {
            // In restricted test environments without full permission change rights
        }
    }

    private static bool CheckIsPackaged()
    {
        try
        {
            int length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            return result != APPMODEL_ERROR_NO_PACKAGE;
        }
        catch
        {
            return false;
        }
    }
}
