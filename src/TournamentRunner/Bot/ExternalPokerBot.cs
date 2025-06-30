using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokerBots.Abstractions;
using TournamentRunner.Logging;

public class ExternalPokerBot : IResettablePokerBot, IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string Name { get; }

    public ExternalPokerBot(string executablePath)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{executablePath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        _stdin.WriteLine("__name__");
        _stdin.Flush();
        Name = _stdout.ReadLine() ?? "UnknownBot";
    }

    public PokerAction GetAction(GameState state)
    {
        string json = JsonSerializer.Serialize(state, _jsonOptions);
        _stdin.WriteLine(json);
        _stdin.Flush();

        var readTask = _stdout.ReadLineAsync();
        if (!readTask.Wait(1000)) // returns as soon as the bot responds or after 1000ms
            throw new BotException(Name, new TimeoutException($"Bot {Name} did not respond within 1000ms."));

        string? response = readTask.Result;
        if (response == null)
            throw new BotException(Name, new Exception($"Bot {Name} failed to respond."));

        Logger.LogDebug(Name + " " + response);

        try
        {
            var resultObject = JsonSerializer.Deserialize<PokerAction>(response, _jsonOptions);
            return resultObject;
        }
        catch (Exception ex)
        {
            throw
                new BotException(
                    Name,
                    new FormatException($"Bot {Name} returned an invalid response that could not be deserialized: '{response}'", ex)
                );
        }
    }

    public void Dispose()
    {
        try { _process.Kill(); } catch { }
        _process.Dispose();
    }
    public void Reset()
    {
        _stdin.WriteLine("__reset__");
        _stdin.Flush();
        _stdout.ReadLine(); // Expect "OK"
    }

}

public class BotException : Exception
{
    public Exception Inner { get;  }
    public string BotName { get; }

    public BotException(string botName, Exception inner)
    {
        BotName = botName;
        Inner = inner;
    }
}