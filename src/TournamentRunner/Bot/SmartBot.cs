using PokerBots.Abstractions;
using System;
using TournamentRunner.Engine;

public class SmartBot : IPokerBot
{
    public string Name => "SmartBot";
    private Random rng = new();

    public PokerAction GetAction(GameState state)
    {
        // Preflop: no community card
        // Postflop: community card is set
        if (state.ToCall == 0 || state.CommunityCard == null)
        {
            // Randomly check/call or raise
            if (state.MyCard.GetValue() > 10)
                return new PokerAction { ActionType = PokerActionType.Call };
            else
                return new PokerAction { ActionType = PokerActionType.Raise, Amount = state.MinRaise };
        }
        else
        {
            int handValue = HandEvaluator.Evaluate(state.MyCard, state.CommunityCard).AbsoluteValue ;
            if (handValue >= 3000)
                return new PokerAction { ActionType = PokerActionType.Raise, Amount = state.MyStack };
            else if (handValue > 2000)
                return new PokerAction { ActionType = PokerActionType.Call, Amount = state.ToCall };
            else 
                return new PokerAction { ActionType = PokerActionType.Fold, Amount = null };
        }
    }
}
