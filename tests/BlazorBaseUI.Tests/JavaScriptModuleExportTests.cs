namespace BlazorBaseUI.Tests;

public class JavaScriptModuleExportTests
{
    [Fact]
    public async Task AnimationsModule_ExportsIndicatorTransitionLifecycleFunctions()
    {
        var source = await File.ReadAllTextAsync(GetRepositoryFile(
            "src",
            "BlazorBaseUI",
            "wwwroot",
            "blazor-baseui-animations.js"));

        ShouldExportFunction(source, "applyStartingStyle");
        ShouldExportFunction(source, "waitForExitTransition");
    }

    [Fact]
    public async Task MenuModule_DoesNotForwardMenubarTriggerKeyboardEvents()
    {
        var source = await File.ReadAllTextAsync(GetRepositoryFile(
            "src",
            "BlazorBaseUI",
            "wwwroot",
            "blazor-baseui-menu.js"));

        source.ShouldNotContain("invokeMethodAsync('OnKeyboardOpen')");
    }

    private static string GetRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorBaseUI.slnx")))
            {
                var parts = new string[pathSegments.Length + 1];
                parts[0] = directory.FullName;
                Array.Copy(pathSegments, 0, parts, 1, pathSegments.Length);

                return Path.Combine(parts);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the BlazorBaseUI repository root.");
    }

    private static void ShouldExportFunction(string source, string functionName)
    {
        var pattern = $@"export\s+(?:async\s+)?function\s+{functionName}\b";
        System.Text.RegularExpressions.Regex.IsMatch(source, pattern).ShouldBeTrue(
            $"Expected blazor-baseui-animations.js to export {functionName}.");
    }
}
