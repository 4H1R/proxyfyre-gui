namespace ProxiFyre.Core.Prereq;

public sealed class PrereqStatus
{
    public bool DriverInstalled { get; init; }
    public bool RuntimeInstalled { get; init; }
    public List<string> Missing { get; } = new();
    public bool AllSatisfied => Missing.Count == 0;
}
