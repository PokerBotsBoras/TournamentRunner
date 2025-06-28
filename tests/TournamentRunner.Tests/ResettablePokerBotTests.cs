using Xunit;
using TournamentRunner;
using PokerBots.Abstractions;
using System.Collections.Generic;
namespace TournamentRunner.Tests;

public class ResettablePokerBotTests
{
    [Fact]
    public void InstanceResettablePokerBot_Reset_CreatesNewInstance()
    {
        // Script: always fold, then always call
        var actions1 = new List<PokerAction> { new PokerAction { ActionType = PokerActionType.Fold } };
        var bot1 = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("A", actions1));
        Assert.Equal(PokerActionType.Fold, bot1.GetAction(new GameState()).ActionType);
        bot1.Reset();
        // After reset, should again fold (new instance, not empty queue)
        Assert.Equal(PokerActionType.Fold, bot1.GetAction(new GameState()).ActionType);
    }

    [Fact]
    public void InstanceResettablePokerBot_ExhaustedScript_ResetsToFullScript()
    {
        var actions = new List<PokerAction> {
            new PokerAction { ActionType = PokerActionType.Fold },
            new PokerAction { ActionType = PokerActionType.Call }
        };
        var bot = new InstanceResettablePokerBot<ScriptedPokerBotWrapper>(() => new ScriptedPokerBotWrapper("B", actions));
        Assert.Equal(PokerActionType.Fold, bot.GetAction(new GameState()).ActionType);
        Assert.Equal(PokerActionType.Call, bot.GetAction(new GameState()).ActionType);
        // Now script is exhausted, next is default call
        Assert.Equal(PokerActionType.Call, bot.GetAction(new GameState()).ActionType);
        bot.Reset();
        // After reset, script is restored
        Assert.Equal(PokerActionType.Fold, bot.GetAction(new GameState()).ActionType);
    }
}

// Helper for lambda-based instance creation
public class ScriptedPokerBotWrapper : IPokerBot
{
    private readonly Queue<PokerAction> _actions;
    public string Name { get; }
    public ScriptedPokerBotWrapper(string name, IEnumerable<PokerAction> actions)
    {
        Name = name;
        _actions = new Queue<PokerAction>(actions);
    }
    public PokerAction GetAction(GameState state)
    {
        if (_actions.Count > 0)
            return _actions.Dequeue();
        return new PokerAction { ActionType = PokerActionType.Call };
    }
}

// Generic instance resettable wrapper for lambda construction
public class InstanceResettablePokerBot<T> : IResettablePokerBot where T : IPokerBot
{
    private readonly System.Func<T> _factory;
    private T _instance;
    public InstanceResettablePokerBot(System.Func<T> factory)
    {
        _factory = factory;
        _instance = _factory();
    }
    public string Name => _instance.Name;
    public PokerAction GetAction(GameState state) => _instance.GetAction(state);
    public void Reset() => _instance = _factory();
}
