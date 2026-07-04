using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class ExcludesViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public ExcludesViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public ObservableCollection<string> Excludes { get; } = new();
    [ObservableProperty] private bool _bypassLan = true;
    [ObservableProperty] private string _newExclude = "";

    public void Load()
    {
        Excludes.Clear();
        var cfg = _store.Read(_configPath);
        foreach (var e in cfg.Excludes) Excludes.Add(e);
        BypassLan = cfg.BypassLan;
    }

    [RelayCommand]
    private void Add()
    {
        var v = NewExclude.Trim();
        if (v.Length > 0 && !Excludes.Contains(v)) Excludes.Add(v);
        NewExclude = "";
    }

    [RelayCommand] private void Remove(string item) => Excludes.Remove(item);

    [RelayCommand]
    private void Save()
    {
        var cfg = _store.Read(_configPath);
        cfg.Excludes = Excludes.ToList();
        cfg.BypassLan = BypassLan;
        _store.Write(_configPath, cfg);
    }
}
