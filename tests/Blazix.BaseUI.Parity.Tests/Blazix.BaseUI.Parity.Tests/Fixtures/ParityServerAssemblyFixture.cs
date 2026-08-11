using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Blazix.BaseUI.Utilities;

namespace Blazix.BaseUI.Parity.Tests.Fixtures;

/// <summary>
/// Assembly-level fixture that starts a single Blazor server for all tests.
/// This fixture is shared across all test classes via xUnit's AssemblyFixture.
/// </summary>
public sealed class ParityServerAssemblyFixture : IAsyncLifetime
{
    private Process? serverProcess;

    /// <summary>
    /// Gets the server address. This is static so all test classes can access it
    /// without needing to receive the fixture instance.
    /// </summary>
    public static string ServerAddress { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Console.WriteLine("[ParityServerAssemblyFixture] Initializing...");

        var projectDir = GetProjectDirectory();
        Console.WriteLine($"[ParityServerAssemblyFixture] Project directory: {projectDir}");

        var port = GetAvailablePort();
        ServerAddress = $"http://127.0.0.1:{port}";

        // Determine configuration from build output path
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var configuration = assemblyLocation.Contains("Release") ? "Release" : "Debug";

        serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build -c {configuration} --no-launch-profile --urls {ServerAddress}",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Environment =
                {
                    // Without Development the host runs in Production, static web assets are
                    // disabled, /_framework/blazor.web.js is served as an empty 200, and the
                    // fixture page never becomes interactive.
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                }
            }
        };

        serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server] {e.Data}");
        };
        serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server Error] {e.Data}");
        };

        serverProcess.Start();
        serverProcess.BeginOutputReadLine();
        serverProcess.BeginErrorReadLine();

        await WaitForServerReadyAsync(ServerAddress, TimeSpan.FromSeconds(60));

        Console.WriteLine($"[ParityServerAssemblyFixture] Initialized. ServerAddress: {ServerAddress}");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[ParityServerAssemblyFixture] Disposing...");

        if (serverProcess is not null && !serverProcess.HasExited)
        {
            try
            {
                serverProcess.Kill(entireProcessTree: true);
                await serverProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParityServerAssemblyFixture] Error stopping server: {ex.Message}");
            }
            finally
            {
                serverProcess.Dispose();
            }
        }

        ServerAddress = string.Empty;
        Console.WriteLine("[ParityServerAssemblyFixture] Disposed.");
    }

    private static string GetProjectDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;

        var projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));

        if (Directory.Exists(projectDir) &&
            File.Exists(Path.Combine(projectDir, "Blazix.BaseUI.Parity.Tests.csproj")))
        {
            return projectDir;
        }

        Console.WriteLine($"[ParityServerAssemblyFixture] Warning: Could not find project directory at {projectDir}");
        return Directory.GetCurrentDirectory();
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [SlopwatchSuppress("SW003", "Empty catch is intentional - server may not be ready yet during polling loop")]
    [SlopwatchSuppress("SW004", "Task.Delay is appropriate for polling server readiness with timeout")]
    private static async Task WaitForServerReadyAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet - continue polling
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Server at {url} did not become ready within {timeout.TotalSeconds} seconds");
    }
}
