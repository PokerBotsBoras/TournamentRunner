// Program.cs
using System;
using TournamentRunner;

class Program
{
    static void Main(string[] args)
    {
        var bots = BotLoader.LoadBots("CompiledBots");
        var tm = new TournamentManager();
        tm.RunAllMatches(bots, Matches: 100, handsPerMatch: 100);
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
    using System.Reflection;
    using PokerBots.Abstractions;

    public static class BotLoader
    {
        public static List<IPokerBot> LoadBots(string folder)
        {
            var bots = new List<IPokerBot>();
            Console.WriteLine($"Looking for bots in: {Path.GetFullPath(folder)}");
            foreach (var dll in Directory.GetFiles(folder, "*.dll"))
            {
                Console.WriteLine($"Found bot DLL: {dll}");
                var asm = Assembly.LoadFrom(dll);
                var types = asm.GetTypes().Where(t => typeof(IPokerBot).IsAssignableFrom(t) && !t.IsInterface);
                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IPokerBot bot)
                        bots.Add(bot);
                }
            }
            return bots;
        }
    }
}

// Runner/TournamentManager.cs
namespace TournamentRunner
{
    using System.Text.RegularExpressions;
    using PokerBots.Abstractions;
    using TournamentRunner.Engine;

    public class TournamentManager
    {
        public void RunAllMatches(List<IPokerBot> bots, int Matches, int handsPerMatch)
        {
            Console.WriteLine($"Running matches for {bots.Count} bots with {handsPerMatch} hands each.");
            foreach (var botA in bots)
            {
                foreach (var botB in bots)
                {
                    if (botA.Name == botB.Name) continue;
                    int botAwins = 0;
                    int botBwins = 0;
                    for (int j = 0; j < Matches; j++)
                    {
                        // Console.WriteLine($"Match: {botA.Name} vs {botB.Name}");
                        var engine = new PokerEngine();
                        int startingStack = 1000;
                        int botXStack = startingStack;
                        int botYStack = startingStack;
                        var botX = botA;
                        var botY = botB;
                        int hands = 0;
                        for (int i = 0; i < handsPerMatch; i++)
                        {
                            hands++;
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

                        if (botXStack == botYStack)
                        {
                            // Console.WriteLine($"  Result: Tie! {botX.Name} {botY.Name}, {botXStack} - {botYStack} after {hands} hands");
                        }
                        var winner = botXStack > botYStack ? botX : botY;
                        // var loser = botXStack < botYStack ? botY : botX;
                        // Console.WriteLine($"  Result: {winner.Name} won over {loser.Name}, {botXStack} - {botYStack} after {hands} hands");
                        if (botXStack != botYStack && winner.Name == botA.Name)
                            botAwins++;
                        else
                            botBwins++;
                    }
                    Console.WriteLine($"  Result:  {botA.Name} : {botAwins} - {botB.Name} : {botBwins}");
                }
            }
        }
    }
}
