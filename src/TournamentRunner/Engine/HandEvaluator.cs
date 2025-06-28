using System;
using System.Collections.Generic;
using PokerBots.Abstractions;

namespace TournamentRunner.Engine
{
    public enum HandRankType
    {
        StraightFlush = 5,
        Straight = 4,
        Flush = 3,
        Pair = 2,
        HighCard = 1
    }

    public class HandRank : IComparable<HandRank>
    {
        public HandRankType Type;
        public int HighValue;
        public int LowValue;

        public int CompareTo(HandRank? other)
        {
            if (other is null) return 1;
            if (Type != other.Type)
                return Type.CompareTo(other.Type);
            if (HighValue != other.HighValue)
                return HighValue.CompareTo(other.HighValue);
            return LowValue.CompareTo(other.LowValue);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not HandRank other) return false;
            return Type == other.Type && HighValue == other.HighValue && LowValue == other.LowValue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, HighValue, LowValue);
        }

        public static bool operator >(HandRank a, HandRank b) => a.CompareTo(b) > 0;
        public static bool operator <(HandRank a, HandRank b) => a.CompareTo(b) < 0;
        public static bool operator ==(HandRank a, HandRank b) => a.Equals(b);
        public static bool operator !=(HandRank a, HandRank b) => !a.Equals(b);
    }

    public static class HandEvaluator
    {
        public static HandRank Evaluate(Card a, Card b)
        {
            bool sameSuit = a.Suit == b.Suit;
            int v1 = a.GetValue();
            int v2 = b.GetValue();
            int high = Math.Max(v1, v2);
            int low = Math.Min(v1, v2);
            bool consecutive = Math.Abs(v1 - v2) == 1;
            bool isPair = v1 == v2;

            // Aces are high only, so A-2 is not a straight
            bool isStraight = consecutive && !(high == 14 && low == 2);
            bool isFlush = sameSuit;

            if (isStraight && isFlush)
                return new HandRank { Type = HandRankType.StraightFlush, HighValue = high, LowValue = low };
            if (isStraight)
                return new HandRank { Type = HandRankType.Straight, HighValue = high, LowValue = low };
            if (isFlush)
                return new HandRank { Type = HandRankType.Flush, HighValue = high, LowValue = low };
            if (isPair)
                return new HandRank { Type = HandRankType.Pair, HighValue = high, LowValue = low };
            return new HandRank { Type = HandRankType.HighCard, HighValue = high, LowValue = low };
        }
    }
}
