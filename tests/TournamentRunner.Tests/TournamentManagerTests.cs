using Xunit;
using TournamentRunner;
using PokerBots.Abstractions;
using System.Collections.Generic;
using TournamentRunner.Engine;
namespace TournamentRunner.Tests;

public class TournamentManagerTests
{
    [Fact]
    public void RunAllRounds_SingleRound_ScriptedBots_WinnerCorrect()
    {
        // BotA always raises, BotB always folds
        var botA = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("Aggro", new[] { new PokerAction { ActionType = PokerActionType.Raise, Amount = 20 } }));
        var botB = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("Passive", new[] { new PokerAction { ActionType = PokerActionType.Fold } }));
        var bots = new List<IResettablePokerBot> { botA, botB };
        var tm = new TournamentManager();
        tm.RunAllRounds(bots, rounds: 1, handsPerRound: 1);
        // Check that results.json exists and Aggro wins
        var json = System.IO.File.ReadAllText("results.json");
        Assert.Contains("Aggro", json);
        Assert.Contains("Passive", json);
    }

    [Fact]
    public void RunAllRounds_MultipleRounds_TiePossible()
    {
        // Both bots always call
        var botA = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("CallerA", new[] { new PokerAction { ActionType = PokerActionType.Call } }));
        var botB = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("CallerB", new[] { new PokerAction { ActionType = PokerActionType.Call } }));
        var bots = new List<IResettablePokerBot> { botA, botB };
        var tm = new TournamentManager();
        tm.RunAllRounds(bots, rounds: 2, handsPerRound: 1);
        var json = System.IO.File.ReadAllText("results.json");
        Assert.Contains("CallerA", json);
        Assert.Contains("CallerB", json);
    }
}
