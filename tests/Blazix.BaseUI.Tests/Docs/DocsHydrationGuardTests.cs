namespace Blazix.BaseUI.Tests.Docs;

public class DocsHydrationGuardTests
{
    [Fact]
    public void AppShell_GatesInteractiveControlsUntilHydration()
    {
        var app = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Components/App.razor");

        app.ShouldContain("blazix-docs-preinteractive");
        app.ShouldContain("window.blazixDocs");
        app.ShouldContain("markInteractive");
        app.ShouldContain("stopImmediatePropagation");
        app.ShouldContain("button, input, select, textarea");
    }

    [Fact]
    public void MainLayout_RemovesHydrationGateAfterFirstInteractiveRender()
    {
        var layout = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/MainLayout.razor");

        layout.ShouldContain("blazixDocs.markInteractive");
        layout.ShouldContain("ObjectDisposedException");
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Blazix.BaseUI.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
