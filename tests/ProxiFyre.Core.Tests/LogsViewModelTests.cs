using ProxiFyre.Core.ViewModels;
using Xunit;

public class LogsViewModelTests
{
    [Fact]
    public void Filter_BySearchText_ReturnsMatchingLines()
    {
        var vm = new LogsViewModel();
        vm.Append("2026 [Info] proxy started for chrome");
        vm.Append("2026 [Error] connect failed 10.0.0.1");
        vm.SearchText = "chrome";
        Assert.Single(vm.FilteredLines);
        Assert.Contains("chrome", vm.FilteredLines[0]);
    }

    [Fact]
    public void Filter_ByLevel_ReturnsMatchingLevel()
    {
        var vm = new LogsViewModel();
        vm.Append("2026 [Info] a");
        vm.Append("2026 [Error] b");
        vm.LevelFilter = "Error";
        Assert.Single(vm.FilteredLines);
        Assert.Contains("[Error]", vm.FilteredLines[0]);
    }

    [Fact]
    public void Filter_All_ReturnsEverything()
    {
        var vm = new LogsViewModel();
        vm.Append("[Info] a"); vm.Append("[Error] b");
        vm.LevelFilter = "All"; vm.SearchText = "";
        Assert.Equal(2, vm.FilteredLines.Count);
    }
}
