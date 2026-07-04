using CommunityToolkit.Mvvm.ComponentModel;
using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.ViewModels;

public partial class RuleItemViewModel : ObservableObject
{
    [ObservableProperty] private string _appNamesText = "";
    [ObservableProperty] private string _endpoint = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _tcp = true;
    [ObservableProperty] private bool _udp = true;

    public static RuleItemViewModel FromModel(ProxyRule r) => new()
    {
        AppNamesText = string.Join(", ", r.AppNames),
        Endpoint = r.Socks5ProxyEndpoint,
        Username = r.Username ?? "",
        Password = r.Password ?? "",
        Tcp = r.SupportedProtocols.Contains("TCP"),
        Udp = r.SupportedProtocols.Contains("UDP"),
    };

    public ProxyRule ToModel()
    {
        var rule = new ProxyRule
        {
            AppNames = AppNamesText.Split(',', StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries).ToList(),
            Socks5ProxyEndpoint = Endpoint.Trim(),
            Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
            Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
            SupportedProtocols = new()
        };
        if (Tcp) rule.SupportedProtocols.Add("TCP");
        if (Udp) rule.SupportedProtocols.Add("UDP");
        return rule;
    }
}
