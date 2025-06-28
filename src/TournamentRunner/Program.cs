// Program.cs
using System;
using TournamentRunner;
using System.Runtime.Loader;
using System.Reflection;
using PokerBots.Abstractions;
using System.Linq;

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

// Engine/PokerEngine.cs
namespace TournamentRunner.Engine
{
    using PokerBots.Abstractions;

    public class PokerEngine
    {
        private static readonly Random rng = new();

        public PokerHandResult PlayHand(IPokerBot botA, IPokerBot botB, int botAStack, int botBStack)
        {
            const int SB = 0, BB = 1;
            var deck = CreateDeck();
            var sbCard = deck[rng.Next(deck.Count)]; deck.Remove(sbCard);
            var bbCard = deck[rng.Next(deck.Count)]; deck.Remove(bbCard);
            var cards = new[] { sbCard, bbCard };
            var bots = new[] { botA, botB };
            int[] stacks = { botAStack - 10, botBStack - 20 };
            int[] toCalls = { 10, 0 };
            int minRaise = 20;
            int pot = 30;
            bool[] isAllIn = { false, false };
            PokerActionType lastAction = PokerActionType.Fold;
            int lastRaise = minRaise;
            List<PokerEvent> actionHistory = new();
            Card? community = null;
            string winner = "";
            bool isTie = false;
            int finalPot = pot;

            // --- Preflop Betting ---
            PreflopBetting();
            // --- Community Card ---
            if (winner == "")
                community = DealCommunityCard(deck);
            // --- Postflop Betting ---
            if (winner == "" && community != null)
                PostflopBetting();
            // --- Showdown or Fold Resolution ---
            if (community != null && winner == "")
                Showdown();
            // --- Add HandResult and Final Callback ---
            AddHandResultAndCallback();
            // --- Return Result ---
            return FinalizeStacks();

            // --- Local Methods ---
            void PreflopBetting()
            {
                int current = SB, other = BB;
                bool allInTriggered = false;
                while (true)
                {
                    var state = BuildGameState(current, null);
                    var action = bots[current].GetAction(state);
                    actionHistory.Add(action);
                    if (action.ActionType == PokerActionType.Fold)
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                    else if (action.ActionType == PokerActionType.Call)
                    {
                        int callAmount = Math.Min(toCalls[current], stacks[current]);
                        stacks[current] -= callAmount; pot += callAmount;
                        toCalls[current] = 0; toCalls[other] = 0;
                        if (isAllIn[current] || isAllIn[other] || lastAction == PokerActionType.Call || allInTriggered)
                            return;
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Call;
                    }
                    else if (action.ActionType == PokerActionType.Raise)
                    {
                        int raiseAmount = action.Amount ?? minRaise;
                        if (raiseAmount < minRaise && raiseAmount < stacks[current])
                            raiseAmount = minRaise;
                        int totalToPut = toCalls[current] + raiseAmount;
                        int maxAllowed = Math.Min(stacks[current], stacks[other] + toCalls[other]);
                        if (totalToPut >= stacks[current])
                        { totalToPut = stacks[current]; isAllIn[current] = true; allInTriggered = true; }
                        else if (totalToPut > maxAllowed)
                        { totalToPut = maxAllowed; isAllIn[current] = true; allInTriggered = true; }
                        stacks[current] -= totalToPut; pot += totalToPut;
                        toCalls[current] = 0; toCalls[other] = totalToPut - toCalls[other];
                        minRaise = Math.Max(raiseAmount, minRaise); lastRaise = raiseAmount;
                        if (allInTriggered)
                        {
                            // Let the other player act once, then end
                            (current, other) = (other, current);
                            var finalState = BuildGameState(current, null);
                            var finalAction = bots[current].GetAction(finalState);
                            actionHistory.Add(finalAction);
                            // Only allow call or fold
                            if (finalAction.ActionType == PokerActionType.Fold)
                            {
                                winner = current == SB ? "SmallBlind" : "BigBlind";
                                isTie = false; finalPot = pot;
                            }
                            // If call, just match chips and end
                            else if (finalAction.ActionType == PokerActionType.Call)
                            {
                                int callAmt = Math.Min(toCalls[current], stacks[current]);
                                stacks[current] -= callAmt; pot += callAmt;
                                toCalls[current] = 0; toCalls[other] = 0;
                            }
                            // If raise, ignore (not allowed when facing all-in)
                            return;
                        }
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Raise;
                    }
                    else
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                }
            }

            Card DealCommunityCard(List<Card> deck)
            {
                var c = deck[rng.Next(deck.Count)];
                deck.Remove(c);
                return c;
            }

            void PostflopBetting()
            {
                int current = SB, other = BB;
                minRaise = 20;
                toCalls = new int[] { 0, 0 };
                isAllIn = new bool[] { stacks[SB] == 0, stacks[BB] == 0 };
                lastAction = PokerActionType.Fold;
                lastRaise = minRaise;
                bool allInTriggered = isAllIn[SB] || isAllIn[BB];
                while (!allInTriggered)
                {
                    var state = BuildGameState(current, community);
                    var action = bots[current].GetAction(state);
                    actionHistory.Add(action);
                    if (action.ActionType == PokerActionType.Fold)
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                    else if (action.ActionType == PokerActionType.Call)
                    {
                        int callAmount = Math.Min(toCalls[current], stacks[current]);
                        stacks[current] -= callAmount; pot += callAmount;
                        toCalls[current] = 0; toCalls[other] = 0;
                        if (lastAction == PokerActionType.Call)
                            return;
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Call;
                    }
                    else if (action.ActionType == PokerActionType.Raise)
                    {
                        int raiseAmount = action.Amount ?? minRaise;
                        if (raiseAmount < minRaise && raiseAmount < stacks[current])
                            raiseAmount = minRaise;
                        int totalToPut = toCalls[current] + raiseAmount;
                        int maxAllowed = Math.Min(stacks[current], stacks[other] + toCalls[other]);
                        if (totalToPut >= stacks[current])
                        { totalToPut = stacks[current]; isAllIn[current] = true; allInTriggered = true; }
                        else if (totalToPut > maxAllowed)
                        { totalToPut = maxAllowed; isAllIn[current] = true; allInTriggered = true; }
                        stacks[current] -= totalToPut; pot += totalToPut;
                        toCalls[current] = 0; toCalls[other] = totalToPut - toCalls[other];
                        minRaise = Math.Max(raiseAmount, minRaise); lastRaise = raiseAmount;
                        if (allInTriggered)
                        {
                            (current, other) = (other, current);
                            var finalState = BuildGameState(current, community);
                            var finalAction = bots[current].GetAction(finalState);
                            actionHistory.Add(finalAction);
                            if (finalAction.ActionType == PokerActionType.Fold)
                            {
                                winner = current == SB ? "SmallBlind" : "BigBlind";
                                isTie = false; finalPot = pot;
                            }
                            else if (finalAction.ActionType == PokerActionType.Call)
                            {
                                int callAmt = Math.Min(toCalls[current], stacks[current]);
                                stacks[current] -= callAmt; pot += callAmt;
                                toCalls[current] = 0; toCalls[other] = 0;
                            }
                            return;
                        }
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Raise;
                    }
                    else
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                }
            }

            void Showdown()
            {
                var rankSB = HandEvaluator.Evaluate(sbCard, community!);
                var rankBB = HandEvaluator.Evaluate(bbCard, community!);
                int cmp = rankSB.CompareTo(rankBB);
                if (cmp > 0) winner = "SmallBlind";
                else if (cmp < 0) winner = "BigBlind";
                else isTie = true;
                finalPot = pot;
            }

            void AddHandResultAndCallback()
            {
                actionHistory.Add(new HandResult {
                    SmallBlindBotCard = sbCard,
                    BigBlindBotCard = bbCard,
                    CommunityCard = community!,
                    Pot = finalPot,
                    Winner = winner,
                    IsTie = isTie
                });
                for (int i = 0; i < 2; i++)
                {
                    var finalState = BuildGameState(i, community);
                    bots[i].GetAction(finalState);
                }
            }

            GameState BuildGameState(int playerIdx, Card? comm)
            {
                return new GameState
                {
                    MyStack = stacks[playerIdx],
                    OpponentStack = stacks[1 - playerIdx],
                    Pot = pot,
                    MyCard = cards[playerIdx],
                    CommunityCard = comm,
                    ToCall = toCalls[playerIdx],
                    MinRaise = minRaise,
                    ActionHistory = new List<PokerEvent>(actionHistory)
                };
            }

            PokerHandResult FinalizeStacks()
            {
                if (winner == "SmallBlind")
                    return new PokerHandResult { BotAStack = stacks[SB] + finalPot, BotBStack = stacks[BB] };
                else if (winner == "BigBlind")
                    return new PokerHandResult { BotAStack = stacks[SB], BotBStack = stacks[BB] + finalPot };
                else
                {
                    stacks[SB] += finalPot / 2;
                    stacks[BB] += finalPot / 2;
                    return new PokerHandResult { BotAStack = stacks[SB], BotBStack = stacks[BB] };
                }
            }
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
                                      .FirstOrDefault();
                if (exeDll != null)
                    botPaths.Add(exeDll);
                else
                    Console.WriteLine($"No bot.dll in {dir}");
            }
            return botPaths;
        }

        public static List<IResettablePokerBot> LoadExternalResettableBots(List<string> botPaths)
        {
            var bots = new List<IResettablePokerBot>();
            foreach (var path in botPaths)
            {
                try
                {
                    bots.Add(new ExternalResettablePokerBot(new ExternalPokerBot(path)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load bot at {path}: {ex.Message}");
                }
            }
            return bots;
        }
    }

}

// Runner/TournamentManager.cs
namespace TournamentRunner
{
    using TournamentRunner.Engine;
    using System.Text.Json;

    public class MatchResult
    {
        public string BotA { get; set; } = "";
        public string BotB { get; set; } = "";
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
        public void RunAllMatches(List<IResettablePokerBot> bots, int matches, int handsPerMatch)
        {
            Console.WriteLine($"Running {matches} matches for {bots.Count} bots with {handsPerMatch} hands each.");
            var results = new List<MatchResult>();
            var disqualified = new HashSet<string>();
            for (int i = 0; i < bots.Count; i++)
            {
                for (int j = 0; j < bots.Count; j++)
                {
                    if (i == j) continue;
                    var botA = bots[i];
                    var botB = bots[j];
                    Console.WriteLine($"=== Starting match: {botA.Name} vs {botB.Name} ===");
                    if (disqualified.Contains(botA.Name) || disqualified.Contains(botB.Name))
                    {
                        Console.WriteLine($"  Skipping match: {botA.Name} vs {botB.Name} (disqualified)");
                        continue;
                    }
                    int botAwins = 0;
                    int botBwins = 0;
                    bool disqualifiedInMatch = false;
                    try
                    {
                        botA.Reset();
                        botB.Reset();
                        for (int m = 0; m < matches; m++)
                        {
                            // if (m < 3 || m % 20 == 0)
                            //     Console.WriteLine($"  [Match {m + 1}/{matches}] {botA.Name} vs {botB.Name}");
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
                                // if (h < 3 || h % 10 == 0)
                                //     Console.WriteLine($"    [Hand {h + 1}/{handsPerMatch}] {botX.Name} (SB) stack: {botXStack}, {botY.Name} (BB) stack: {botYStack}");
                                try
                                {
                                    var result = engine.PlayHand(botX, botY, botXStack, botYStack);
                                    botXStack = result.BotAStack;
                                    botYStack = result.BotBStack;
                                    (botX, botY) = (botY, botX);
                                    (botXStack, botYStack) = (botYStack, botXStack);
                                    // if (h < 3 || h % 10 == 0)
                                    //     Console.WriteLine($"    [Hand {h + 1}] End stacks: {botA.Name}: {botXStack}, {botB.Name}: {botYStack}");
                                    if (botXStack <= 0 || botYStack <= 0)
                                        break;
                                }
                                catch (BotException ex)
                                {
                                    Console.WriteLine($"    Bot '{ex.BotName}' disqualified during hand {h + 1}: {ex.Inner.Message}");
                                    disqualified.Add(ex.BotName);
                                    disqualifiedInMatch = true;
                                    break;
                                }
                            }
                            if (disqualifiedInMatch)
                            {
                                Console.WriteLine($"  Ending match early due to disqualification.");
                                break;
                            }
                            if (botXStack != botYStack)
                            {
                                var winner = botXStack > botYStack ? botX : botY;
                                // Console.WriteLine($"  [Match {m + 1}] Winner: {winner.Name}");
                                if (winner.Name == botA.Name)
                                    botAwins++;
                                else
                                    botBwins++;
                            }
                            else
                            {
                                Console.WriteLine($"  [Match {m + 1}] Tie");
                            }
                        }
                        if (!disqualifiedInMatch)
                        {
                            results.Add(new MatchResult
                            {
                                BotA = botA.Name,
                                BotB = botB.Name,
                                BotAWins = botAwins,
                                BotBWins = botBwins
                            });
                            Console.WriteLine($"=== {botAwins} {botA.Name} - {botB.Name} {botBwins} ===");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error running match between {botA.Name} and {botB.Name}: {ex.Message}");
                    }
                }
            }

            // Save results to file
            SaveResults(results);
        }

        private void SaveResults(List<MatchResult> results)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var fileName = $"results_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json";
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(new TournamentResults { Date = date, Results = results }, options);
            File.WriteAllText(fileName, json);
            File.WriteAllText("results.json", json);
            Console.WriteLine($"Results saved to {fileName} and results.json");
        }
    }
}
