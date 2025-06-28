using Xunit;
using PokerBots.Abstractions;
using TournamentRunner.Engine;

namespace TournamentRunner.Tests;

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
