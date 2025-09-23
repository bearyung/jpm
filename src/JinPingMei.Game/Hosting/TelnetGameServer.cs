using System.Net;
using System.Net.Sockets;
using System.Text;
using JinPingMei.Engine;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetGameServer
{
    private readonly IPEndPoint _endpoint;
    private TcpListener? _listener;
    private readonly List<Task> _clientTasks = new();
    private readonly object _sync = new();

    public TelnetGameServer(IPEndPoint endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        _listener = new TcpListener(_endpoint);
        _listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                var task = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
                RegisterClientTask(task);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }

        await Task.WhenAll(GetClientTasksSnapshot());
    }

    public async Task StopAsync()
    {
        _listener?.Stop();
        _listener = null;

        await Task.WhenAll(GetClientTasksSnapshot());
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var connection = client;
        using var stream = connection.GetStream();
        using var registration = cancellationToken.Register(() => connection.Close());
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };

        var session = new GameSession(GameRuntime.CreateDefault());

        await writer.WriteLineAsync("歡迎來到《金瓶梅》互動敘事實驗。");
        await writer.WriteLineAsync(" Type 'help' for prototype commands. Type 'quit' to disconnect.");
        await writer.WriteLineAsync(string.Empty);
        await writer.WriteLineAsync(session.RenderIntro());

        while (!cancellationToken.IsCancellationRequested)
        {
            await writer.WriteAsync("> ");
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }

            var trimmed = line.Trim();

            if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("再見，期待下次相會。");
                break;
            }

            var response = session.HandleCommand(trimmed);
            await writer.WriteLineAsync(response);
        }
    }

    private void RegisterClientTask(Task task)
    {
        lock (_sync)
        {
            _clientTasks.Add(task);
        }

        task.ContinueWith(_ =>
        {
            lock (_sync)
            {
                _clientTasks.Remove(task);
            }
        }, TaskScheduler.Default);
    }

    private Task[] GetClientTasksSnapshot()
    {
        lock (_sync)
        {
            return _clientTasks.ToArray();
        }
    }
}
