using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PasswordManager.Desktop.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace PasswordManager.Desktop;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        this.InitializeComponent();

        ApplyTheme(ViewModel.IsDarkTheme);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ViewModel.StatusMessage = "Settings view is deferred to subsequent milestones.";
            return;
        }

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            ViewModel.SelectNavigationCommand.Execute(tag);
        }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleThemeCommand.Execute(null);
        ApplyTheme(ViewModel.IsDarkTheme);
    }

    private void ApplyTheme(bool isDark)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedItem is not null)
        {
            ViewModel.ToggleFavoriteCommand.Execute(ViewModel.SelectedItem);
        }
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel.SelectedItemUsername)) return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.SelectedItemUsername);
        Clipboard.SetContent(dataPackage);
        ViewModel.StatusMessage = $"Copied username for '{ViewModel.SelectedItemTitle}'.";
    }

    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedItem is null) return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.SelectedItem.Password);
        Clipboard.SetContent(dataPackage);
        ViewModel.StatusMessage = $"Copied synthetic password for '{ViewModel.SelectedItemTitle}'.";
    }
}
