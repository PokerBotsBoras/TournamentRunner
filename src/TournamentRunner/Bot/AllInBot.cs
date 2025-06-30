using System;
using PokerBots.Abstractions;

public class AllInBot : IPokerBot
{
    private static readonly Random rng = new();

    public string Name => "All In Bot";

    // GetAction is called by the game runner, you need to return what action the bot should take, based on the GameState
    public PokerAction GetAction(GameState state)
    {
    	return new PokerAction { ActionType = PokerActionType.Raise, Amount = state.MyStack };
    }
}
