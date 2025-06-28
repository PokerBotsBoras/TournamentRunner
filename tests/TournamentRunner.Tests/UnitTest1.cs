using System;
using System.Collections.Generic;
using TournamentRunner;
using TournamentRunner.Engine;
using Xunit;
using PokerBots.Abstractions;

namespace TournamentRunner.Tests;

public class ThrowingExternalPokerBot : IResettablePokerBot
{
    public string Name { get; set; } = "ThrowBot";
    public PokerAction GetAction(GameState state)
    {
        throw new BotException(Name, new Exception("Always throws!"));
    }
    public void Reset() { }
}

public class DisqualificationTests
{
    [Fact]
    public void ExternalPokerBot_ThatThrows_GetsDisqualified()
    {
        var bots = new List<IResettablePokerBot>
        {
            new ThrowingExternalPokerBot(),
            new InstanceResettablePokerBot<RandomBot>(() => new RandomBot())
        };
        var tm = new TournamentManager();
        tm.RunAllMatches(bots, matches: 2, handsPerMatch: 2);
        var resultsJson = System.IO.File.ReadAllText("results.json");
        Assert.DoesNotContain("ThrowBot", resultsJson); // results.json should be empty
        // Should print disqualification message (not checked here, but can be checked in logs)
    }
}

public class UnitTest1
{
    [Fact]
    public void RandomBot_Hand_DoesNotThrow()
    {
        var bot = new RandomBot();
        var engine = new PokerEngine();
        // Both bots are RandomBot for simplicity
        PokerHandResult result = engine.PlayHand(bot, bot, 1000, 1000);
        Assert.InRange(result.BotAStack, 0, 2000);
        Assert.InRange(result.BotBStack, 0, 2000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }
}
