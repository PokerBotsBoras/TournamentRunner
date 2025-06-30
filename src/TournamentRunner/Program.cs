using System;
using System.Linq;
using TournamentRunner;
using TournamentRunner.Logging;

class Program
{
    static void Main(string[] args)
    {
        // Parse verbosity level from command line arguments
        LogLevel logLevel = LogLevel.Info; // Default level
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--verbosity" || args[i] == "-v")
            {
                if (i + 1 < args.Length)
                {
                    if (Enum.TryParse<LogLevel>(args[i + 1], true, out var level))
                    {
                        logLevel = level;
                    }
                    else if (int.TryParse(args[i + 1], out var levelNum) && levelNum >= 0 && levelNum <= 5)
                    {
                        logLevel = (LogLevel)levelNum;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid verbosity level: {args[i + 1]}");
                        Console.WriteLine("Valid levels: Silent(0), Error(1), Warning(2), Info(3), Debug(4), Verbose(5)");
                        return;
                    }
                }
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                ShowHelp();
                return;
            }
        }
        
        // Configure logger
        Logger.Configure(new ConsoleLogger(logLevel));
        
        Logger.LogInfo($"Starting Tournament Runner with verbosity level: {logLevel}");
        var bots = new List<IResettablePokerBot>();

        string botsDir = args.Length > 0 ? args[0] : "CompiledBots";
        var botPaths = BotLoader.LoadExecutableBots(botsDir);
        bots.AddRange(BotLoader.LoadExternalResettableBots(botPaths));

        var dockerBot = new DockerPokerBot("poker-bot-python");
        bots.Add(dockerBot);

        bots.Add(new InstanceResettablePokerBot<RandomBot>());
        bots.Add(new InstanceResettablePokerBot<SmartBot>());
        bots.Add(new InstanceResettablePokerBot<AllInBot>());

        Logger.LogInfo(string.Join("\n", bots.Select(b => b.Name)));
        var tm = new TournamentManager();
        tm.RunAllMatches(bots, matches: 100, handsPerMatch: 100);
    }

    static void ShowHelp()
    {
        Console.WriteLine("Tournament Runner - Poker Bot Tournament System");
        Console.WriteLine();
        Console.WriteLine("Usage: TournamentRunner [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -v, --verbosity <level>    Set verbosity level");
        Console.WriteLine("                             0: Silent - No output");
        Console.WriteLine("                             1: Error - Only errors");
        Console.WriteLine("                             2: Warning - Errors and warnings");
        Console.WriteLine("                             3: Info - Basic information (default)");
        Console.WriteLine("                             4: Debug - Detailed information");
        Console.WriteLine("                             5: Verbose - Full poker engine details");
        Console.WriteLine("  -h, --help                 Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  TournamentRunner --verbosity 5    # Full verbose output");
        Console.WriteLine("  TournamentRunner -v Debug         # Debug level output");
        Console.WriteLine("  TournamentRunner -v 0             # Silent mode");
    }
}
