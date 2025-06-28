using PokerBots.Abstractions;
using System;

public class RandomBot : IPokerBot
{
    public string Name => "RandomBot";
    private Random rng = new();

    public PokerAction GetAction(GameState state)
    {
        // Preflop: no community card
        // Postflop: community card is set
        if (state.ToCall == 0)
        {
            // Randomly check/call or raise
            if (rng.NextDouble() < 0.7)
                return new PokerAction { ActionType = PokerActionType.Call };
            else
                return new PokerAction { ActionType = PokerActionType.Raise, Amount = state.MinRaise };
        }
        else
        {
            // Randomly fold, call, or raise
            double x = rng.NextDouble();
            if (x < 0.1)
                return new PokerAction { ActionType = PokerActionType.Fold };
            else if (x < 0.8)
                return new PokerAction { ActionType = PokerActionType.Call };
            else
                return new PokerAction { ActionType = PokerActionType.Raise, Amount = state.MinRaise };
        }
    }
}
