using System;
using System.Linq;
using TournamentRunner;

class Program
{
    static void Main(string[] args)
    {
        string botsDir = args.Length > 0 ? args[0] : "CompiledBots";
        var botPaths = BotLoader.LoadExecutableBots(botsDir);
        var bots = BotLoader.LoadExternalResettableBots(botPaths);
        bots.Add(new InstanceResettablePokerBot<RandomBot>());
        bots.Add(new InstanceResettablePokerBot<RandomBot>());
        Console.WriteLine(string.Join("\n", bots.Select(b => b.Name)));
        var tm = new TournamentManager();
        tm.RunAllMatches(bots, matches: 100, handsPerMatch: 100);
    }
}
