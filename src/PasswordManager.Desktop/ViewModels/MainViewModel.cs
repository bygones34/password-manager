using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Desktop.Models;
using PasswordManager.Desktop.Services;

namespace PasswordManager.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly List<SyntheticVaultItem> _sourceItems = [];

    [ObservableProperty]
    public partial ObservableCollection<SyntheticVaultItem> FilteredItems { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedItemUsername))]
    [NotifyPropertyChangedFor(nameof(SelectedItemWebsiteUrl))]
    [NotifyPropertyChangedFor(nameof(SelectedItemCategory))]
    [NotifyPropertyChangedFor(nameof(SelectedItemNotes))]
    [NotifyPropertyChangedFor(nameof(DisplayedPassword))]
    public partial SyntheticVaultItem? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedNavTag { get; set; } = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedPassword))]
    [NotifyPropertyChangedFor(nameof(RevealButtonLabel))]
    public partial bool IsPasswordRevealed { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Synthetic Demo Vault (M0.4 Shell)";

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; } = true;

    // Derived / Computed Properties for clean XAML binding
    public bool HasSelectedItem => SelectedItem is not null;
    public string SelectedItemTitle => SelectedItem?.Title ?? "Select an item";
    public string SelectedItemUsername => SelectedItem?.Username ?? string.Empty;
    public string SelectedItemWebsiteUrl => SelectedItem?.WebsiteUrl ?? string.Empty;
    public string SelectedItemCategory => SelectedItem?.Category ?? "General";
    public string SelectedItemNotes => SelectedItem?.Notes ?? string.Empty;
    public string RevealButtonLabel => IsPasswordRevealed ? "Hide" : "Reveal";

    public string DisplayedPassword
    {
        get
        {
            if (SelectedItem is null) return string.Empty;
            return IsPasswordRevealed ? SelectedItem.Password : SelectedItem.MaskedPassword;
        }
    }

    public MainViewModel()
    {
        LoadSyntheticData();
    }

    private void LoadSyntheticData()
    {
        _sourceItems.Clear();
        _sourceItems.AddRange(SyntheticVaultService.GetSampleItems());
        ApplyFilter();
        SelectedItem = FilteredItems.FirstOrDefault();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedNavTagChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedItemChanged(SyntheticVaultItem? value)
    {
        IsPasswordRevealed = false;
    }

    [RelayCommand]
    public void TogglePasswordReveal()
    {
        IsPasswordRevealed = !IsPasswordRevealed;
    }

    [RelayCommand]
    public void ToggleFavorite(SyntheticVaultItem? item)
    {
        if (item is null) return;

        var index = _sourceItems.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
        {
            var updated = item with { IsFavorite = !item.IsFavorite };
            _sourceItems[index] = updated;
            ApplyFilter();
            SelectedItem = _sourceItems.FirstOrDefault(i => i.Id == item.Id);
            StatusMessage = updated.IsFavorite ? $"Added '{updated.Title}' to favorites." : $"Removed '{updated.Title}' from favorites.";
        }
    }

    [RelayCommand]
    public void SelectNavigation(string tag)
    {
        SelectedNavTag = tag;
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        StatusMessage = IsDarkTheme ? "Switched to Dark Theme." : "Switched to Light Theme.";
    }

    private void ApplyFilter()
    {
        IEnumerable<SyntheticVaultItem> items = _sourceItems;

        // Apply category / tag filter
        items = SelectedNavTag switch
        {
            "Favorites" => items.Where(i => i.IsFavorite),
            "Logins" => items.Where(i => i.Category == "Logins"),
            "Servers" => items.Where(i => i.Category == "Servers"),
            "Notes" => items.Where(i => i.Category == "Notes"),
            _ => items
        };

        // Apply search query
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            items = items.Where(i =>
                i.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.WebsiteUrl.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Notes.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var result = items.ToList();
        FilteredItems = new ObservableCollection<SyntheticVaultItem>(result);

        if (SelectedItem is not null && !FilteredItems.Contains(SelectedItem))
        {
            SelectedItem = FilteredItems.FirstOrDefault();
        }
    }
}
