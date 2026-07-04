using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class RulesViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public RulesViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public ObservableCollection<RuleItemViewModel> Rules { get; } = new();
    [ObservableProperty] private string _errorText = "";

    public void Load()
    {
        Rules.Clear();
        var cfg = _store.Read(_configPath);
        foreach (var r in cfg.Proxies) Rules.Add(RuleItemViewModel.FromModel(r));
    }

    [RelayCommand] private void AddRule() => Rules.Add(new RuleItemViewModel());

    [RelayCommand] private void DeleteRule(RuleItemViewModel item) => Rules.Remove(item);

    [RelayCommand]
    private void Save()
    {
        ErrorText = "";
        var cfg = _store.Read(_configPath);      // preserve logLevel/bypassLan/excludes/unknown fields
        cfg.Proxies = Rules.Select(r => r.ToModel()).ToList();

        var result = _store.Validate(cfg);
        if (!result.IsValid)
        {
            ErrorText = string.Join(Environment.NewLine, result.Errors);
            return;
        }
        _store.Write(_configPath, cfg);
    }
}
