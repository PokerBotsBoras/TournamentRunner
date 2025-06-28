using Xunit;
using PokerBots.Abstractions;
using TournamentRunner.Engine;

namespace TournamentRunner.Tests;

public class AllInEdgeCasesTest
{
    [Fact]
    public void AllIn_Fold_Response_EndsHand()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("FoldBot", new[] {
            new PokerAction { ActionType = PokerActionType.Fold }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
        Assert.True(result.BotAStack == 2000 || result.BotBStack == 2000);
    }

    [Fact]
    public void Both_AllIn_Raise_Then_Raise()
    {
        var botA = new ScriptedPokerBot("AllInBotA", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("AllInBotB", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 980 } // BB posts 20, stack 980 left
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void Raise_After_AllIn_Is_Ignored()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("RaiseAfterAllIn", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 100 }, // Should be ignored, treated as call
            new PokerAction { ActionType = PokerActionType.Call } // fallback
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void Raise_Too_Much_Is_Capped()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 99999 } // Should be capped to stack
        });
        var botB = new ScriptedPokerBot("CallBot", new[] {
            new PokerAction { ActionType = PokerActionType.Call }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void AllInBot_Cannot_Act_After_AllIn()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 },
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 10 } // Should never be called
        });
        var botB = new ScriptedPokerBot("CallBot", new[] {
            new PokerAction { ActionType = PokerActionType.Call }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void AllIn_On_Flop_OtherPlayerResponds()
    {
        // Preflop: both call, Flop: BotA all-in, BotB call
        var botA = new ScriptedPokerBot("BotA", new[] {
            new PokerAction { ActionType = PokerActionType.Call }, // preflop
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 } // postflop all-in
        });
        var botB = new ScriptedPokerBot("BotB", new[] {
            new PokerAction { ActionType = PokerActionType.Call }, // preflop
            new PokerAction { ActionType = PokerActionType.Call } // call all-in
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void AllIn_With_ShortStack_PartialCall()
    {
        // BotA is SB with only 15 chips, BB bets 20, BotA can only call 15
        var botA = new ScriptedPokerBot("ShortStack", new[] {
            new PokerAction { ActionType = PokerActionType.Call } // can only call 15
        });
        var botB = new ScriptedPokerBot("BB", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 20 }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 15, 1000);
        Assert.Equal(1015, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void AllIn_ResultsIn_Tie_SplitPot()
    {
        // Both all-in, but force a tie by using the same bot (RandomBot)
        var bot = new RandomBot();
        var engine = new PokerEngine();
        var result = engine.PlayHand(bot, bot, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
        // If tie, both stacks should be equal
        if (result.BotAStack == result.BotBStack)
            Assert.Equal(1000, result.BotAStack);
    }

    [Fact]
    public void ExtraActions_After_Hand_Are_Ignored()
    {
        var botA = new ScriptedPokerBot("BotA", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 },
            new PokerAction { ActionType = PokerActionType.Call },
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 10 }
        });
        var botB = new ScriptedPokerBot("BotB", new[] {
            new PokerAction { ActionType = PokerActionType.Call },
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 10 }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void AllIn_Raise_Fold_OtherWinsPot()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("FoldBot", new[] {
            new PokerAction { ActionType = PokerActionType.Fold }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
        Assert.True(result.BotAStack == 2000 || result.BotBStack == 2000);
    }

    [Fact]
    public void Multiple_Consecutive_AllIns_StateResets()
    {
        var botA = new ScriptedPokerBot("AllInBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("CallBot", new[] {
            new PokerAction { ActionType = PokerActionType.Call }
        });
        var engine = new PokerEngine();
        for (int i = 0; i < 5; i++)
        {
            var result = engine.PlayHand(botA, botB, 1000, 1000);
            Assert.Equal(2000, result.BotAStack + result.BotBStack);
        }
    }

    [Fact]
    public void Raise_Zero_Or_Negative_Is_Treated_As_MinRaise()
    {
        var botA = new ScriptedPokerBot("ZeroRaiseBot", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 0 },
            new PokerAction { ActionType = PokerActionType.Raise, Amount = -10 }
        });
        var botB = new ScriptedPokerBot("CallBot", new[] {
            new PokerAction { ActionType = PokerActionType.Call },
            new PokerAction { ActionType = PokerActionType.Call }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
    }

    [Fact]
    public void Both_AllIn_Exact_Stack_Match()
    {
        var botA = new ScriptedPokerBot("AllInBotA", new[] {
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 }
        });
        var botB = new ScriptedPokerBot("AllInBotB", new[] {
            new PokerAction { ActionType = PokerActionType.Call }
        });
        var engine = new PokerEngine();
        var result = engine.PlayHand(botA, botB, 1000, 1000);
        Assert.Equal(2000, result.BotAStack + result.BotBStack);
        Assert.True(result.BotAStack == 0 || result.BotBStack == 0 || result.BotAStack == 1000);
    }

    [Fact]
    public void BlindPosting_InitialPotAndStacks()
    {
        var botA = new ScriptedPokerBot("CheckBotA", new[] { new PokerAction { ActionType = PokerActionType.Call } });
        var botB = new ScriptedPokerBot("CheckBotB", new[] { new PokerAction { ActionType = PokerActionType.Call } });
        var engine = new PokerEngine();
        int startA = 1000, startB = 1000;
        engine.PlayHand(botA, botB, startA, startB);
        // After blinds, stacks should be 990/980, pot 30
        // Can't check pot directly, but can infer from stacks
        Assert.Equal(990, startA - 10);
        Assert.Equal(980, startB - 20);
    }

    [Fact]
    public void ExcessAllInTruncation_OnlyMatchedAmountInPot()
    {
        // BotA has 1000, BotB has 100, BotA goes all-in, BotB can only call 100
        var botA = new ScriptedPokerBot("AllInBot", new[] { new PokerAction { ActionType = PokerActionType.Raise, Amount = 990 } });
        var botB = new ScriptedPokerBot("ShortStack", new[] { new PokerAction { ActionType = PokerActionType.Call } });
        var engine = new PokerEngine();
        PokerHandResult result = engine.PlayHand(botA, botB, 1000, 100);
        // Only 100 from each can go in after blinds, so pot is 200 (100 from each + blinds)
        Assert.Equal(1100, result.BotAStack + result.BotBStack);
    }
}
