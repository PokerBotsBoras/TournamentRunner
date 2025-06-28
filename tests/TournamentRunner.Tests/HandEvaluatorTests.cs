using Xunit;
using TournamentRunner.Engine;
using PokerBots.Abstractions;

namespace TournamentRunner.Tests;

public class HandEvaluatorTests
{
    [Fact]
    public void RankOrdering_StraightFlush_Beats_Straight_Beats_Flush_Beats_Pair_Beats_HighCard()
    {
        // Example cards (using only two cards per hand as per your engine)
        var community = new Card { Rank = "K", Suit = "♠" };
        var straightFlush = HandEvaluator.Evaluate(new Card { Rank = "Q", Suit = "♠" }, community);
        var straight = HandEvaluator.Evaluate(new Card { Rank = "J", Suit = "♠" }, new Card { Rank = "10", Suit = "♣" });
        var flush = HandEvaluator.Evaluate(new Card { Rank = "2", Suit = "♠" }, community);
        var pair = HandEvaluator.Evaluate(new Card { Rank = "K", Suit = "♦" }, community);
        var highCard = HandEvaluator.Evaluate(new Card { Rank = "9", Suit = "♣" }, community);
        Assert.True(straightFlush > straight);
        Assert.True(straight > flush);
        Assert.True(flush > pair);
        Assert.True(pair > highCard);
    }

    [Fact]
    public void AceHigh_Only_A2_IsNotStraight_AK_IsStraight()
    {
        var ace = new Card { Rank = "A", Suit = "♠" };
        var two = new Card { Rank = "2", Suit = "♣" };
        var king = new Card { Rank = "K", Suit = "♦" };
        var straightAK = HandEvaluator.Evaluate(ace, king);
        var notStraightA2 = HandEvaluator.Evaluate(ace, two);
        Assert.True(straightAK > notStraightA2);
    }

    [Fact]
    public void TieDetection_IdenticalHands_SplitPot()
    {
        var card1 = new Card { Rank = "Q", Suit = "♠" };
        var card2 = new Card { Rank = "Q", Suit = "♣" };
        var rank1 = HandEvaluator.Evaluate(card1, card2);
        var rank2 = HandEvaluator.Evaluate(card2, card1);
        Assert.Equal(rank1, rank2);
    }
}
