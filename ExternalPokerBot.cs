using System.Diagnostics;
using System.Text.Json;
using PokerBots.Abstractions;

public class ExternalPokerBot : IPokerBot, IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;

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
        string json = JsonSerializer.Serialize(state);
        _stdin.WriteLine(json);
        _stdin.Flush();

        string? response = _stdout.ReadLine();
        if (response == null)
            throw new Exception($"Bot {Name} failed to respond.");

        return JsonSerializer.Deserialize<PokerAction>(response)!;
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