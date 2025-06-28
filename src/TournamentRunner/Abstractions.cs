using System.Collections.Generic;

namespace PokerBots.Abstractions;

// Bots/IPokerBot.cs
// namespace TournamentRunner.Bots

public interface IPokerBot
{
    string Name { get; }
    PokerAction GetAction(GameState state);
}


// Bots/PokerAction.cs
// namespace TournamentRunner.Bots

public enum PokerActionType { Fold, Call, Raise }

public abstract class PokerEvent { }

public class PokerAction : PokerEvent
{
    public PokerActionType ActionType { get; set; }
    public int? Amount { get; set; } // Only for Raise
}

public class HandResult : PokerEvent
{
    //Card of the bot that had the SB
    public Card SmallBlindBotCard { get; set; } = null!; 
    //Card of the bot that has the BB
    public Card BigBlindBotCard { get; set; } = null!;
    public Card CommunityCard { get; set; } = null!;
    public int Pot { get; set; }
    public string Winner { get; set; } = string.Empty; // or enum if preferred
    public bool IsTie { get; set; }
    // Optionally: public List<PokerAction> Actions { get; set; }
}


// Bots/GameState.cs
// namespace TournamentRunner.Bots

public class GameState
{
    public int MyStack { get; set; }
    public int OpponentStack { get; set; }
    public int Pot { get; set; }
    public Card MyCard { get; set; } = null!;
    public Card? CommunityCard { get; set; }
    public int ToCall { get; set; }
    public int MinRaise { get; set; }
    //The first action belongs to the bot that had the SB, if there is 0 objects 
    // in this list when the bot gets this objects, you have smallblind, 
    // ActionHistory.Count % 2 == 0 is small blind
    // ActionHistory.Count % 2 == 1 is big blind
    public List<PokerEvent> ActionHistory { get; set; } = new();
}

public class Card
{
    public string Rank { get; set; } = null!; // "2" through "A"
    public string Suit { get; set; } = null!; // "♠", "♦", etc

    public int GetValue()
    {
        return Rank switch
        {
            "2" => 2,
            "3" => 3,
            "4" => 4,
            "5" => 5,
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => 0
        };
    }

    public override string ToString() => $"{Rank}{Suit}";
}

