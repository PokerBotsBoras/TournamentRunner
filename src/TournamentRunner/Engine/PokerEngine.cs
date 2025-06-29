// Engine/PokerEngine.cs
namespace TournamentRunner.Engine
{
    using PokerBots.Abstractions;

    public class PokerEngine
    {
        private static readonly Random rng = new();

        public PokerHandResult PlayHand(IPokerBot botA, IPokerBot botB, int botAStack, int botBStack)
        {
            const int SB = 0, BB = 1;
            var deck = CreateDeck();
            var sbCard = deck[rng.Next(deck.Count)]; deck.Remove(sbCard);
            var bbCard = deck[rng.Next(deck.Count)]; deck.Remove(bbCard);
            var cards = new[] { sbCard, bbCard };
            var bots = new[] { botA, botB };
            int[] stacks = { botAStack - 10, botBStack - 20 };
            int[] toCalls = { 10, 0 };
            int minRaise = 20;
            int pot = 30;
            bool[] isAllIn = { false, false };
            PokerActionType lastAction = PokerActionType.Fold;
            int lastRaise = minRaise;
            List<PokerEvent> actionHistory = new();
            Card? community = null;
            string winner = "";
            bool isTie = false;
            int finalPot = pot;

            // --- Preflop Betting ---
            PreflopBetting();
            // --- Community Card ---
            if (winner == "")
                community = DealCommunityCard(deck);
            // --- Postflop Betting ---
            if (winner == "" && community != null)
                PostflopBetting();
            // --- Showdown or Fold Resolution ---
            if (community != null && winner == "")
                Showdown();
            // --- Add HandResult and Final Callback ---
            AddHandResultAndCallback();
            // --- Return Result ---
            return FinalizeStacks();

            // --- Local Methods ---
            void PreflopBetting()
            {
                int current = SB, other = BB;
                bool allInTriggered = false;
                while (true)
                {
                    var state = BuildGameState(current, null);
                    var action = bots[current].GetAction(state);
                    actionHistory.Add(action);
                    if (action.ActionType == PokerActionType.Fold)
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                    else if (action.ActionType == PokerActionType.Call)
                    {
                        int callAmount = Math.Min(toCalls[current], stacks[current]);
                        stacks[current] -= callAmount; pot += callAmount;
                        toCalls[current] = 0; toCalls[other] = 0;
                        if (isAllIn[current] || isAllIn[other] || lastAction == PokerActionType.Call || allInTriggered)
                            return;
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Call;
                    }
                    else if (action.ActionType == PokerActionType.Raise)
                    {
                        int raiseAmount = action.Amount ?? minRaise;
                        if (raiseAmount < minRaise && raiseAmount < stacks[current])
                            raiseAmount = minRaise;
                        int totalToPut = toCalls[current] + raiseAmount;
                        int maxAllowed = Math.Min(stacks[current], stacks[other] + toCalls[other]);
                        if (totalToPut >= stacks[current])
                        { totalToPut = stacks[current]; isAllIn[current] = true; allInTriggered = true; }
                        else if (totalToPut > maxAllowed)
                        { totalToPut = maxAllowed; isAllIn[current] = true; allInTriggered = true; }
                        stacks[current] -= totalToPut; pot += totalToPut;
                        toCalls[current] = 0; toCalls[other] = totalToPut - toCalls[other];
                        minRaise = Math.Max(raiseAmount, minRaise); lastRaise = raiseAmount;
                        if (allInTriggered)
                        {
                            // Let the other player act once, then end
                            (current, other) = (other, current);
                            var finalState = BuildGameState(current, null);
                            var finalAction = bots[current].GetAction(finalState);
                            actionHistory.Add(finalAction);
                            // Only allow call or fold
                            if (finalAction.ActionType == PokerActionType.Fold)
                            {
                                winner = current == SB ? "SmallBlind" : "BigBlind";
                                isTie = false; finalPot = pot;
                            }
                            // If call, just match chips and end
                            else if (finalAction.ActionType == PokerActionType.Call)
                            {
                                int callAmt = Math.Min(toCalls[current], stacks[current]);
                                stacks[current] -= callAmt; pot += callAmt;
                                toCalls[current] = 0; toCalls[other] = 0;
                            }
                            // If raise, ignore (not allowed when facing all-in)
                            return;
                        }
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Raise;
                    }
                    else
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                }
            }

            Card DealCommunityCard(List<Card> deck)
            {
                var c = deck[rng.Next(deck.Count)];
                deck.Remove(c);
                return c;
            }

            void PostflopBetting()
            {
                int current = SB, other = BB;
                minRaise = 20;
                toCalls = new int[] { 0, 0 };
                isAllIn = new bool[] { stacks[SB] == 0, stacks[BB] == 0 };
                lastAction = PokerActionType.Fold;
                lastRaise = minRaise;
                bool allInTriggered = isAllIn[SB] || isAllIn[BB];
                while (!allInTriggered)
                {
                    var state = BuildGameState(current, community);
                    var action = bots[current].GetAction(state);
                    actionHistory.Add(action);
                    if (action.ActionType == PokerActionType.Fold)
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                    else if (action.ActionType == PokerActionType.Call)
                    {
                        int callAmount = Math.Min(toCalls[current], stacks[current]);
                        stacks[current] -= callAmount; pot += callAmount;
                        toCalls[current] = 0; toCalls[other] = 0;
                        if (lastAction == PokerActionType.Call)
                            return;
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Call;
                    }
                    else if (action.ActionType == PokerActionType.Raise)
                    {
                        int raiseAmount = action.Amount ?? minRaise;
                        if (raiseAmount < minRaise && raiseAmount < stacks[current])
                            raiseAmount = minRaise;
                        int totalToPut = toCalls[current] + raiseAmount;
                        int maxAllowed = Math.Min(stacks[current], stacks[other] + toCalls[other]);
                        if (totalToPut >= stacks[current])
                        { totalToPut = stacks[current]; isAllIn[current] = true; allInTriggered = true; }
                        else if (totalToPut > maxAllowed)
                        { totalToPut = maxAllowed; isAllIn[current] = true; allInTriggered = true; }
                        stacks[current] -= totalToPut; pot += totalToPut;
                        toCalls[current] = 0; toCalls[other] = totalToPut - toCalls[other];
                        minRaise = Math.Max(raiseAmount, minRaise); lastRaise = raiseAmount;
                        if (allInTriggered)
                        {
                            (current, other) = (other, current);
                            var finalState = BuildGameState(current, community);
                            var finalAction = bots[current].GetAction(finalState);
                            actionHistory.Add(finalAction);
                            if (finalAction.ActionType == PokerActionType.Fold)
                            {
                                winner = current == SB ? "SmallBlind" : "BigBlind";
                                isTie = false; finalPot = pot;
                            }
                            else if (finalAction.ActionType == PokerActionType.Call)
                            {
                                int callAmt = Math.Min(toCalls[current], stacks[current]);
                                stacks[current] -= callAmt; pot += callAmt;
                                toCalls[current] = 0; toCalls[other] = 0;
                            }
                            return;
                        }
                        (current, other) = (other, current);
                        lastAction = PokerActionType.Raise;
                    }
                    else
                    {
                        winner = current == SB ? "SmallBlind" : "BigBlind";
                        isTie = false; finalPot = pot;
                        return;
                    }
                }
            }

            void Showdown()
            {
                var rankSB = HandEvaluator.Evaluate(sbCard, community!);
                var rankBB = HandEvaluator.Evaluate(bbCard, community!);
                int cmp = rankSB.CompareTo(rankBB);
                if (cmp > 0) winner = "SmallBlind";
                else if (cmp < 0) winner = "BigBlind";
                else isTie = true;
                finalPot = pot;
            }

            void AddHandResultAndCallback()
            {
                actionHistory.Add(new HandResult {
                    SmallBlindBotCard = sbCard,
                    BigBlindBotCard = bbCard,
                    CommunityCard = community!,
                    Pot = finalPot,
                    Winner = winner,
                    IsTie = isTie
                });
                for (int i = 0; i < 2; i++)
                {
                    var finalState = BuildGameState(i, community);
                    bots[i].GetAction(finalState);
                }
            }

            GameState BuildGameState(int playerIdx, Card? comm)
            {
                return new GameState
                {
                    MyStack = stacks[playerIdx],
                    OpponentStack = stacks[1 - playerIdx],
                    Pot = pot,
                    MyCard = cards[playerIdx],
                    CommunityCard = comm,
                    ToCall = toCalls[playerIdx],
                    MinRaise = minRaise,
                    ActionHistory = new List<PokerEvent>(actionHistory)
                };
            }

            PokerHandResult FinalizeStacks()
            {
                if (winner == "SmallBlind")
                    return new PokerHandResult { BotAStack = stacks[SB] + finalPot, BotBStack = stacks[BB] };
                else if (winner == "BigBlind")
                    return new PokerHandResult { BotAStack = stacks[SB], BotBStack = stacks[BB] + finalPot };
                else
                {
                    stacks[SB] += finalPot / 2;
                    stacks[BB] += finalPot / 2;
                    return new PokerHandResult { BotAStack = stacks[SB], BotBStack = stacks[BB] };
                }
            }
        }

        private List<Card> CreateDeck()
        {
            var suits = new[] { "♠", "♦", "♣", "♥" };
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var deck = new List<Card>();
            foreach (var suit in suits)
                foreach (var rank in ranks)
                    deck.Add(new Card { Rank = rank, Suit = suit });
            return deck;
        }
    }

    public class PokerHandResult
    {
        public int BotAStack;
        public int BotBStack;
    }
}
