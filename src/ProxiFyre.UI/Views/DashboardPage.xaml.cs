using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel Vm { get; }
    public DashboardPage()
    {
        Vm = App.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
        Vm.Load();
    }
}
