// Program.cs
using System;
using TournamentRunner;
using System.Runtime.Loader;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        var botPaths = BotLoader.LoadExecutableBots("CompiledBots");
        Console.WriteLine(string.Join("\n", botPaths));
        var tm = new TournamentManager();
        tm.RunAllMatches(botPaths, matches: 100, handsPerMatch: 100);
    }
}

// Engine/PokerEngine.cs
namespace TournamentRunner.Engine
{
    using PokerBots.Abstractions;

    public class PokerEngine
    {
        private static readonly Random rng = new();

        public PokerHandResult PlayHand(IPokerBot botA, IPokerBot botB, int botAStack, int botBStack)
        {
            var deck = CreateDeck();
            var cardA = deck[rng.Next(deck.Count)];
            deck.Remove(cardA);
            var cardB = deck[rng.Next(deck.Count)];
            deck.Remove(cardB);

            int pot = 30; // SB: 10, BB: 20
            int aStack = botAStack - 10;
            int bStack = botBStack - 20;
            int toCallA = 10;
            int toCallB = 0;
            int minRaise = 20;

            var cards = new[] { cardA, cardB };
            var bots = new[] { botA, botB };
            var stacks = new[] { aStack, bStack };
            var toCalls = new[] { toCallA, toCallB };

            int current = 0; // 0: botA, 1: botB
            int other = 1;
            bool handOver = false;
            PokerActionType lastAction = PokerActionType.Fold;

            while (!handOver)
            {
                var state = new GameState
                {
                    MyStack = stacks[current],
                    OpponentStack = stacks[other],
                    Pot = pot,
                    MyCard = cards[current],
                    ToCall = toCalls[current],
                    MinRaise = minRaise
                };

                var action = bots[current].GetAction(state);

                if (action.ActionType == PokerActionType.Fold)
                {
                    handOver = true;
                    // Winner is the other bot, they win the pot
                    if (current == 0)
                        return new PokerHandResult { BotAStack = stacks[0], BotBStack = stacks[1] + pot };
                    else
                        return new PokerHandResult { BotAStack = stacks[0] + pot, BotBStack = stacks[1] };
                }
                else if (action.ActionType == PokerActionType.Call)
                {
                    int callAmount = toCalls[current];
                    if (callAmount > stacks[current])
                    {
                        // Tried to call with too many chips: disqualify (fold)
                        handOver = true;
                        if (current == 0)
                            return new PokerHandResult { BotAStack = stacks[0], BotBStack = stacks[1] + pot };
                        else
                            return new PokerHandResult { BotAStack = stacks[0] + pot, BotBStack = stacks[1] };
                    }
                    stacks[current] -= callAmount;
                    pot += callAmount;
                    toCalls[current] = 0;
                    toCalls[other] = 0;
                    // If both have called, showdown
                    if (lastAction == PokerActionType.Call)
                        handOver = true;
                    else
                    {
                        // Switch turn
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Call;
                    }
                }
                else if (action.ActionType == PokerActionType.Raise)
                {
                    int raiseAmount = Math.Max(action.Amount ?? 0, minRaise);
                    int totalToPut = toCalls[current] + raiseAmount;
                    if (totalToPut > stacks[current])
                    {
                        // Tried to raise with too many chips: disqualify (fold)
                        handOver = true;
                        if (current == 0)
                            return new PokerHandResult { BotAStack = stacks[0], BotBStack = stacks[1] + pot };
                        else
                            return new PokerHandResult { BotAStack = stacks[0] + pot, BotBStack = stacks[1] };
                    }
                    stacks[current] -= totalToPut;
                    pot += totalToPut;
                    toCalls[current] = 0;
                    toCalls[other] = raiseAmount;
                    minRaise = raiseAmount;
                    // Switch turn
                    (current, other) = (other, current);
                    lastAction = PokerActionType.Raise;
                }
                else
                {
                    // Invalid action, treat as fold
                    handOver = true;
                    if (current == 0)
                        return new PokerHandResult { BotAStack = stacks[0], BotBStack = stacks[1] + pot };
                    else
                        return new PokerHandResult { BotAStack = stacks[0] + pot, BotBStack = stacks[1] };
                }
            }

            // Showdown
            return cards[0].GetValue() > cards[1].GetValue()
                ? new PokerHandResult { BotAStack = stacks[0] + pot, BotBStack = stacks[1] }
                : new PokerHandResult { BotAStack = stacks[0], BotBStack = stacks[1] + pot };

        }

        private List<Card> CreateDeck()
        {
            var suits = new[] { "♠", "♦", "♣", "♥" };
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var deck = new List<Card>();
            foreach (var suit in suits)
                foreach (var rank in ranks)
                    deck.Add(new Card { Rank = rank, Suit = suit });
            return deck;
        }
    }

    public class PokerHandResult
    {
        public int BotAStack;
        public int BotBStack;
    }
}

// Runner/BotLoader.cs
namespace TournamentRunner
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    public static class BotLoader
    {
        public static List<string> LoadExecutableBots(string root)
        {
            var botPaths = new List<string>();

            foreach (var dir in Directory.GetDirectories(root))
            {
                var exeDll = Directory.GetFiles(dir, "bot.dll")
                                      .FirstOrDefault(); // Pick the first .dll, regardless of name
                
                if (exeDll != null)
                    botPaths.Add(exeDll);
                else
                    Console.WriteLine($"No bot.dll in {dir}");
            }

            return botPaths;
        }
    }

}

// Runner/TournamentManager.cs
namespace TournamentRunner
{
    using System.Text.RegularExpressions;
    using PokerBots.Abstractions;
    using TournamentRunner.Engine;
    using System.Text.Json;

    public class MatchResult
    {
        public string BotA { get; set; }
        public string BotB { get; set; }
        public int BotAWins { get; set; }
        public int BotBWins { get; set; }
    }

    public class TournamentResults
    {
        public string Date { get; set; } = "";
        public List<TournamentRunner.MatchResult> Results { get; set; } = new();
    }
    public class TournamentManager
    {
        public void RunAllMatches(List<string> botPaths, int matches, int handsPerMatch)
        {
            Console.WriteLine($"Running {matches} matches for {botPaths.Count} bots with {handsPerMatch} hands each.");
            var results = new List<MatchResult>();

            var bots = botPaths
                .Select(path => new ExternalPokerBot(path))
                .ToList();

            try
            {
                for (int i = 0; i < bots.Count; i++)
                {
                    for (int j = 0; j < bots.Count; j++)
                    {
                        if (i == j) continue;

                        var botA = bots[i];
                        var botB = bots[j];

                        botA.Reset();
                        botB.Reset();

                        int botAwins = 0;
                        int botBwins = 0;

                        for (int m = 0; m < matches; m++)
                        {
                            botA.Reset();
                            botB.Reset();

                            var engine = new PokerEngine();
                            int startingStack = 1000;
                            int botXStack = startingStack;
                            int botYStack = startingStack;
                            var botX = botA;
                            var botY = botB;

                            for (int h = 0; h < handsPerMatch; h++)
                            {
                                if (botXStack < 10 || botYStack < 20)
                                    break;

                                var result = engine.PlayHand(botX, botY, botXStack, botYStack);
                                botXStack = result.BotAStack;
                                botYStack = result.BotBStack;

                                (botX, botY) = (botY, botX);
                                (botXStack, botYStack) = (botYStack, botXStack);

                                if (botXStack <= 0 || botYStack <= 0)
                                    break;
                            }

                            if (botXStack != botYStack)
                            {
                                var winner = botXStack > botYStack ? botX : botY;
                                if (winner.Name == botA.Name)
                                    botAwins++;
                                else
                                    botBwins++;
                            }
                        }

                        Console.WriteLine($"  Result:  {botA.Name} : {botAwins} - {botB.Name} : {botBwins}");

                        results.Add(new MatchResult
                        {
                            BotA = botA.Name,
                            BotB = botB.Name,
                            BotAWins = botAwins,
                            BotBWins = botBwins
                        });
                    }
                }
            }
            finally
            {
                foreach (var bot in bots)
                    bot.Dispose();
            }

            var output = new TournamentResults
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Results = results
            };

            var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("tournament_results.json", json);
            Console.WriteLine("Results written to tournament_results.json");
        }
    }

}
