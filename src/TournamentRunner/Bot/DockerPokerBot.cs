using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokerBots.Abstractions;
using System.Threading.Tasks;

public class DockerPokerBot : IResettablePokerBot, IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private readonly Task _stderrReaderTask;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public string Name { get; }

    public DockerPokerBot(string imageName, string? command = null)
    {
        // Build docker run command
        string dockerArgs = $"run -i --rm {imageName}";
        if (!string.IsNullOrWhiteSpace(command))
            dockerArgs += $" {command}";

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = dockerArgs,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        // Read stderr in the background to prevent buffer blocking
        _stderrReaderTask = Task.Run(async () => {
            var stderr = _process.StandardError;
            char[] buffer = new char[4096];
            while (!stderr.EndOfStream)
            {
                await stderr.ReadAsync(buffer, 0, buffer.Length);
                // Optionally, log or discard the output
            }
        });

        _stdin.WriteLine("__name__");
        _stdin.Flush();
        Name = _stdout.ReadLine() ?? "UnknownDockerBot";
    }

    public PokerAction GetAction(GameState state)
    {
        string json = JsonSerializer.Serialize(state, _jsonOptions);
        _stdin.WriteLine(json);
        _stdin.Flush();

        var readTask = _stdout.ReadLineAsync();
        if (!readTask.Wait(1000))
            throw new BotException(Name, new TimeoutException($"Bot {Name} did not respond within 1000ms."));

        string? response = readTask.Result;
        if (response == null)
            throw new BotException(Name, new Exception($"Bot {Name} failed to respond."));

        try
        {
            return JsonSerializer.Deserialize<PokerAction>(response, _jsonOptions)!;
        }
        catch (Exception ex)
        {
            throw new BotException(
                Name,
                new FormatException($"Bot {Name} returned an invalid response that could not be deserialized: '{response}'", ex)
            );
        }
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
                _process.Kill();
        }
        catch { }
        _process.Dispose();
        try { _stderrReaderTask.Wait(500); } catch { }
    }

    public void Reset()
    {
        _stdin.WriteLine("__reset__");
        _stdin.Flush();
        _stdout.ReadLine(); // Expect "OK"
    }
}
