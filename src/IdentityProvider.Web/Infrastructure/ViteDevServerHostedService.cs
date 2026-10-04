using System.Diagnostics;
using System.Net.Sockets;

namespace IdentityProvider.Web.Infrastructure;

/// <summary>
/// Starts Vite in Development so the ASP.NET host can reverse-proxy the SPA
/// and keep the browser on the OpenIddict issuer origin.
/// </summary>
public sealed class ViteDevServerHostedService : IHostedService, IDisposable
{
    public const int Port = 5173;

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ViteDevServerHostedService> _logger;
    private Process? _process;

    public ViteDevServerHostedService(
        IWebHostEnvironment environment,
        ILogger<ViteDevServerHostedService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        if (IsPortOpen())
        {
            _logger.LogInformation("Vite is already listening on port {Port}.", Port);
            return;
        }

        var clientDir = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "client"));
        if (!File.Exists(Path.Combine(clientDir, "package.json")))
        {
            _logger.LogWarning("SPA client was not found at {ClientDir}.", clientDir);
            return;
        }

        var viteJs = Path.Combine(clientDir, "node_modules", "vite", "bin", "vite.js");
        if (!File.Exists(viteJs))
        {
            _logger.LogError("Vite is not installed. Run npm install in {ClientDir}.", clientDir);
            return;
        }

        var startInfo = new ProcessStartInfo("node", $"\"{viteJs}\"")
        {
            WorkingDirectory = clientDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["BROWSER"] = "none";

        _process = Process.Start(startInfo);
        if (_process is null)
        {
            _logger.LogError("Could not start the Vite dev server.");
            return;
        }

        _process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogInformation("Vite: {Message}", e.Data);
            }
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogWarning("Vite: {Message}", e.Data);
            }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _logger.LogInformation("Starting Vite in {ClientDir} (pid {Pid}).", clientDir, _process.Id);

        var until = DateTime.UtcNow.AddSeconds(60);
        while (!IsPortOpen())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow > until || _process.HasExited)
            {
                _logger.LogError("Vite did not start on port {Port} in time.", Port);
                return;
            }

            await Task.Delay(400, cancellationToken);
        }

        _logger.LogInformation("Vite is ready at http://localhost:{Port}.", Port);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not stop Vite.");
            }
        }

        _process?.Dispose();
        _process = null;
    }

    private static bool IsPortOpen()
    {
        try
        {
            foreach (var host in new[] { "127.0.0.1", "localhost" })
            {
                try
                {
                    using var client = new TcpClient();
                    var task = client.ConnectAsync(host, Port);
                    if (task.Wait(TimeSpan.FromMilliseconds(300)) && client.Connected)
                    {
                        return true;
                    }
                }
                catch
                {
                    // try the next host
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
