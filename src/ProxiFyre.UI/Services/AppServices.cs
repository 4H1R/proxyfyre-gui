using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Prereq;
using ProxiFyre.Core.Service;
using ProxiFyre.UI.Platform;

namespace ProxiFyre.UI.Services;

public static class AppServices
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILocatorService>(_ => new LocatorService());
        services.AddSingleton<IConfigStore, ConfigStore>();
        services.AddSingleton<ISystemProbe, WindowsSystemProbe>();
        services.AddSingleton<IPrereqChecker>(sp =>
            new PrereqChecker(sp.GetRequiredService<ISystemProbe>()));
        services.AddSingleton<IServiceHost, WindowsServiceHost>();
        services.AddSingleton<IServiceController>(sp =>
            new ServiceController(sp.GetRequiredService<IServiceHost>()));

        // Viewmodels registered in Task 13+.
        RegisterViewModels(services);

        return services.BuildServiceProvider();
    }

    // Extended in later tasks (partial-like; edit this method as viewmodels are added).
    private static void RegisterViewModels(IServiceCollection services)
    {
    }
}
