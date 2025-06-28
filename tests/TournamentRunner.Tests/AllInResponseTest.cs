using Xunit;
using PokerBots.Abstractions;
using TournamentRunner.Engine;

namespace TournamentRunner.Tests;

public class AllInResponseTest
{
    [Fact]
    public void AllIn_Triggers_OtherPlayerFinalAction_AndEndsBetting()
    {
        // BotA: Raise all-in preflop, BotB: Call
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 } // all-in (SB posts 10, stack 990 left)
        });
        var botB = new ScriptedPokerBot("CallBot", new[] {
            new PokerAction { ActionType = PokerActionType.Call } // call all-in
        });
        var engine = new PokerEngine();
        // Both start with 1000, SB posts 10, BB posts 20
        PokerHandResult result = engine.PlayHand(botA, botB, 1000, 1000);
        // After all-in and call, all chips should be in the pot, one winner gets 2000 or split
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
        Assert.True(result.BotAStack == 0 || result.BotBStack == 0 || result.BotAStack == 1000); // Only valid outcomes
    }
}
