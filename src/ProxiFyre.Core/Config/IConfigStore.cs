using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.Config;

public interface IConfigStore
{
    AppConfig Read(string path);
    void Write(string path, AppConfig config);
    ValidationResult Validate(AppConfig config);
}
