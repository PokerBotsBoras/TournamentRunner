using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using PokerBots.Abstractions;
using System.Text.Json.Serialization;

namespace TournamentRunner.Tests;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    [Fact]
    public void Card_SerializesCorrectly()
    {
        var card = new Card { Rank = "A", Suit = "♠" };
        
        string json = JsonSerializer.Serialize(card, _options);
        var deserialized = JsonSerializer.Deserialize<Card>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(card.Rank, deserialized.Rank);
        Assert.Equal(card.Suit, deserialized.Suit);
        Assert.Equal(14, deserialized.GetValue());
    }

    [Fact]
    public void PokerAction_Fold_SerializesCorrectly()
    {
        var action = new PokerAction
        {
            ActionType = PokerActionType.Fold,
            Amount = null
        };
        
        string json = JsonSerializer.Serialize(action, _options);
        var deserialized = JsonSerializer.Deserialize<PokerAction>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(PokerActionType.Fold, deserialized.ActionType);
        Assert.Null(deserialized.Amount);
    }

    [Fact]
    public void PokerAction_Call_SerializesCorrectly()
    {
        var action = new PokerAction
        {
            ActionType = PokerActionType.Call,
            Amount = null
        };
        
        string json = JsonSerializer.Serialize(action, _options);
        var deserialized = JsonSerializer.Deserialize<PokerAction>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(PokerActionType.Call, deserialized.ActionType);
        Assert.Null(deserialized.Amount);
    }

    [Fact]
    public void PokerAction_Raise_SerializesCorrectly()
    {
        var action = new PokerAction
        {
            ActionType = PokerActionType.Raise,
            Amount = 100
        };
        
        string json = JsonSerializer.Serialize(action, _options);
        var deserialized = JsonSerializer.Deserialize<PokerAction>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(PokerActionType.Raise, deserialized.ActionType);
        Assert.Equal(100, deserialized.Amount);
    }

    [Fact]
    public void GameState_SerializesCorrectly()
    {
        var gameState = new GameState
        {
            MyStack = 1000,
            OpponentStack = 950,
            Pot = 50,
            MyCard = new Card { Rank = "K", Suit = "♥" },
            CommunityCard = new Card { Rank = "A", Suit = "♠" },
            ToCall = 25,
            MinRaise = 50,
            ActionHistory = new List<PokerAction>
            {
                new PokerAction { ActionType = PokerActionType.Call, Amount = null },
                new PokerAction { ActionType = PokerActionType.Raise, Amount = 50 }
            }
        };
        
        string json = JsonSerializer.Serialize(gameState, _options);
        var deserialized = JsonSerializer.Deserialize<GameState>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(gameState.MyStack, deserialized.MyStack);
        Assert.Equal(gameState.OpponentStack, deserialized.OpponentStack);
        Assert.Equal(gameState.Pot, deserialized.Pot);
        Assert.Equal(gameState.MyCard.Rank, deserialized.MyCard.Rank);
        Assert.Equal(gameState.MyCard.Suit, deserialized.MyCard.Suit);
        Assert.NotNull(deserialized.CommunityCard);
        Assert.Equal(gameState.CommunityCard.Rank, deserialized.CommunityCard.Rank);
        Assert.Equal(gameState.CommunityCard.Suit, deserialized.CommunityCard.Suit);
        Assert.Equal(gameState.ToCall, deserialized.ToCall);
        Assert.Equal(gameState.MinRaise, deserialized.MinRaise);
        Assert.Equal(2, deserialized.ActionHistory.Count);
    }

    [Fact]
    public void GameState_WithNullCommunityCard_SerializesCorrectly()
    {
        var gameState = new GameState
        {
            MyStack = 1000,
            OpponentStack = 1000,
            Pot = 0,
            MyCard = new Card { Rank = "7", Suit = "♦" },
            CommunityCard = null,
            ToCall = 0,
            MinRaise = 25,
            ActionHistory = new List<PokerAction>()
        };
        
        string json = JsonSerializer.Serialize(gameState, _options);
        var deserialized = JsonSerializer.Deserialize<GameState>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(gameState.MyStack, deserialized.MyStack);
        Assert.Equal(gameState.OpponentStack, deserialized.OpponentStack);
        Assert.Null(deserialized.CommunityCard);
        Assert.Empty(deserialized.ActionHistory);
    }

    [Fact]
    public void HandResult_SerializesCorrectly()
    {
        var handResult = new HandResult
        {
            SmallBlindBotCard = new Card { Rank = "Q", Suit = "♣" },
            BigBlindBotCard = new Card { Rank = "J", Suit = "♥" },
            CommunityCard = new Card { Rank = "A", Suit = "♠" },
            Pot = 200,
            Winner = "PlayerA",
            IsTie = false
        };
        
        string json = JsonSerializer.Serialize(handResult, _options);
        var deserialized = JsonSerializer.Deserialize<HandResult>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(handResult.SmallBlindBotCard.Rank, deserialized.SmallBlindBotCard.Rank);
        Assert.Equal(handResult.BigBlindBotCard.Suit, deserialized.BigBlindBotCard.Suit);
        Assert.Equal(handResult.CommunityCard.Rank, deserialized.CommunityCard.Rank);
        Assert.Equal(handResult.Pot, deserialized.Pot);
        Assert.Equal(handResult.Winner, deserialized.Winner);
        Assert.Equal(handResult.IsTie, deserialized.IsTie);
    }

    [Fact]
    public void ActionHistory_WithMixedPokerActions_SerializesCorrectly()
    {
        var actionHistory = new List<PokerAction>
        {
            new PokerAction { ActionType = PokerActionType.Call, Amount = null },
            new PokerAction { ActionType = PokerActionType.Raise, Amount = 75 },
            new PokerAction { ActionType = PokerActionType.Fold, Amount = null }
        };
        
        string json = JsonSerializer.Serialize(actionHistory, _options);
        var deserialized = JsonSerializer.Deserialize<List<PokerAction>>(json, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal(PokerActionType.Call, deserialized[0].ActionType);
        Assert.Equal(PokerActionType.Raise, deserialized[1].ActionType);
        Assert.Equal(75, deserialized[1].Amount);
        Assert.Equal(PokerActionType.Fold, deserialized[2].ActionType);
    }

    [Fact]
    public void Card_SpecialCharacters_SerializesCorrectly()
    {
        var cards = new[]
        {
            new Card { Rank = "A", Suit = "♠" },
            new Card { Rank = "K", Suit = "♥" },
            new Card { Rank = "Q", Suit = "♦" },
            new Card { Rank = "J", Suit = "♣" }
        };
        
        foreach (var card in cards)
        {
            string json = JsonSerializer.Serialize(card, _options);
            var deserialized = JsonSerializer.Deserialize<Card>(json, _options);
            
            Assert.NotNull(deserialized);
            Assert.Equal(card.Rank, deserialized.Rank);
            Assert.Equal(card.Suit, deserialized.Suit);
        }
    }

    [Fact]
    public void PokerActionType_Enum_SerializesToInteger_ByDefault()
    {
        var action = new PokerAction { ActionType = PokerActionType.Fold, Amount = null };
        
        string json = JsonSerializer.Serialize(action, _options);
        
        // By default, System.Text.Json serializes enums as integers
        Assert.Contains("\"ActionType\": 0", json); // Fold = 0
        Assert.DoesNotContain("\"Fold\"", json);
    }

    [Fact]
    public void PokerActionType_Enum_WithStringConverter_SerializesToString()
    {
        var stringOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var action = new PokerAction { ActionType = PokerActionType.Fold, Amount = null };
        
        string json = JsonSerializer.Serialize(action, stringOptions);
        
        // With JsonStringEnumConverter, enums serialize as strings
        Assert.Contains("\"ActionType\": \"Fold\"", json);
        Assert.DoesNotContain("\"ActionType\": 0", json);
    }

    [Fact]
    public void PokerAction_CanDeserialize_BothIntegerAndStringEnums()
    {
        // Test integer enum (default)
        string integerJson = """{"ActionType":0,"Amount":null}""";
        var fromInteger = JsonSerializer.Deserialize<PokerAction>(integerJson, _options);
        
        Assert.NotNull(fromInteger);
        Assert.Equal(PokerActionType.Fold, fromInteger.ActionType);
        
        // Test string enum - this should fail with default options
        string stringJson = """{"ActionType":"Fold","Amount":null}""";
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<PokerAction>(stringJson, _options));
    }

    [Fact]
    public void PokerAction_WithStringEnumConverter_CanDeserializeBothFormats()
    {
        var stringOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        
        // Test string enum
        string stringJson = """{"ActionType":"Fold","Amount":null}""";
        var fromString = JsonSerializer.Deserialize<PokerAction>(stringJson, stringOptions);
        
        Assert.NotNull(fromString);
        Assert.Equal(PokerActionType.Fold, fromString.ActionType);
        
        // Test integer enum - this should also work with JsonStringEnumConverter
        string integerJson = """{"ActionType":0,"Amount":null}""";
        var fromInteger = JsonSerializer.Deserialize<PokerAction>(integerJson, stringOptions);
        
        Assert.NotNull(fromInteger);
        Assert.Equal(PokerActionType.Fold, fromInteger.ActionType);
    }

    private static string GetSamplesDirFilePath(string fileName)
    {
        var dir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "SerializedSamples");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, fileName);
    }

    [Fact]
    public void GameState_SerializeAndSave_Sample1()
    {
        var gameState = new GameState
        {
            MyStack = 1000,
            OpponentStack = 950,
            Pot = 50,
            MyCard = new Card { Rank = "K", Suit = "♥" },
            CommunityCard = new Card { Rank = "A", Suit = "♠" },
            ToCall = 25,
            MinRaise = 50,
            ActionHistory = new List<PokerAction>
            {
                new PokerAction { ActionType = PokerActionType.Call, Amount = null },
                new PokerAction { ActionType = PokerActionType.Raise, Amount = 50 }
            }
        };
        var noIndentOptions = new JsonSerializerOptions();
        string json = JsonSerializer.Serialize(gameState, noIndentOptions);
        System.IO.File.WriteAllText(GetSamplesDirFilePath("GameState_Sample1.json"), json);
    }

    [Fact]
    public void GameState_SerializeAndSave_Sample2_NullCommunityCard()
    {
        var gameState = new GameState
        {
            MyStack = 800,
            OpponentStack = 1200,
            Pot = 100,
            MyCard = new Card { Rank = "7", Suit = "♦" },
            CommunityCard = null,
            ToCall = 0,
            MinRaise = 25,
            ActionHistory = new List<PokerAction>()
        };
        var noIndentOptions = new JsonSerializerOptions();
        string json = JsonSerializer.Serialize(gameState, noIndentOptions);
        System.IO.File.WriteAllText(GetSamplesDirFilePath("GameState_Sample2.json"), json);
    }

    [Fact]
    public void GameState_SerializeAndSave_Sample3_EmptyActionHistory()
    {
        var gameState = new GameState
        {
            MyStack = 500,
            OpponentStack = 500,
            Pot = 0,
            MyCard = new Card { Rank = "2", Suit = "♣" },
            CommunityCard = new Card { Rank = "5", Suit = "♠" },
            ToCall = 10,
            MinRaise = 20,
            ActionHistory = new List<PokerAction>()
        };
        var noIndentOptions = new JsonSerializerOptions();
        string json = JsonSerializer.Serialize(gameState, noIndentOptions);
        System.IO.File.WriteAllText(GetSamplesDirFilePath("GameState_Sample3.json"), json);
    }
}
