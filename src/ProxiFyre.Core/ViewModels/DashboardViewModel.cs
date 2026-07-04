using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Prereq;

namespace ProxiFyre.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IPrereqChecker _checker;
    private readonly IConfigStore _store;
    private readonly ILocatorService _locator;

    public DashboardViewModel(IPrereqChecker checker, IConfigStore store, ILocatorService locator)
    {
        _checker = checker; _store = store; _locator = locator;
    }

    [ObservableProperty] private int _ruleCount;
    [ObservableProperty] private bool _prereqReady;
    public ObservableCollection<string> MissingPrereqs { get; } = new();

    public void Load()
    {
        MissingPrereqs.Clear();
        var status = _checker.Check();
        PrereqReady = status.AllSatisfied;
        foreach (var m in status.Missing) MissingPrereqs.Add(m);

        RuleCount = _locator.ConfigExists ? _store.Read(_locator.ConfigPath).Proxies.Count : 0;
    }

    [RelayCommand] private void Recheck() => Load();
}
