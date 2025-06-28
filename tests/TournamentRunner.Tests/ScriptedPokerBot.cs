using System.Collections.Generic;
using PokerBots.Abstractions;

namespace TournamentRunner.Tests;

public class ScriptedPokerBot : IPokerBot
{
    private readonly Queue<PokerAction> _actions;
    public string Name { get; }

    public ScriptedPokerBot(string name, IEnumerable<PokerAction> actions)
    {
        Name = name;
        _actions = new Queue<PokerAction>(actions);
    }

    public PokerAction GetAction(GameState state)
    {
        if (_actions.Count > 0)
            return _actions.Dequeue();
        // Default to call if script runs out
        return new PokerAction { ActionType = PokerActionType.Call };
    }
}
