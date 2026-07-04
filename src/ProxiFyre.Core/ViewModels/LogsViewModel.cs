using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProxiFyre.Core.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly List<string> _all = new();

    public ObservableCollection<string> FilteredLines { get; } = new();

    [ObservableProperty] private string _levelFilter = "All";
    [ObservableProperty] private string _searchText = "";

    partial void OnLevelFilterChanged(string value) => Reapply();
    partial void OnSearchTextChanged(string value) => Reapply();

    public void Append(string line)
    {
        _all.Add(line);
        if (Matches(line)) FilteredLines.Add(line);
    }

    private bool Matches(string line)
    {
        if (LevelFilter != "All" && !line.Contains($"[{LevelFilter}]", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(SearchText)
            && !line.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private void Reapply()
    {
        FilteredLines.Clear();
        foreach (var l in _all) if (Matches(l)) FilteredLines.Add(l);
    }
}
