using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class ExcludesPage : Page
{
    public ExcludesViewModel Vm { get; }
    public ExcludesPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new ExcludesViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }
}
