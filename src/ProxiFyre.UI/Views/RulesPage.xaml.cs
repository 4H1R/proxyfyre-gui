using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class RulesPage : Page
{
    public RulesViewModel Vm { get; }
    public RulesPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new RulesViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }
}
