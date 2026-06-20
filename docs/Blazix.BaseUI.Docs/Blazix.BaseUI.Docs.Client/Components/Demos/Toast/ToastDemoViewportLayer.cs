using System.Threading;

namespace Blazix.BaseUI.Docs.Client.Components.Demos.Toast;

internal static class ToastDemoViewportLayer
{
    private const int InitialLayer = 20;

    private static int currentLayer = InitialLayer;

    public static int Next() => Interlocked.Increment(ref currentLayer);

    public static string Style(int layer) => $"z-index: {layer}";
}
