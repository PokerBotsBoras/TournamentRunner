using PokerBots.Abstractions;
using System;

namespace TournamentRunner
{
    // Wrapper interface for bots that can be reset
    public interface IResettablePokerBot : IPokerBot
    {
        void Reset();
    }

    // Wraps an ExternalPokerBot and calls its Reset()
    public class ExternalResettablePokerBot : IResettablePokerBot
    {
        private readonly ExternalPokerBot _bot;
        public ExternalResettablePokerBot(ExternalPokerBot bot)
        {
            _bot = bot;
        }
        public string Name => _bot.Name;
        public PokerAction GetAction(GameState state) => _bot.GetAction(state);
        public void Reset() => _bot.Reset();
    }

    // Wraps any IPokerBot type, recreates instance on Reset
    public class InstanceResettablePokerBot<T> : IResettablePokerBot where T : IPokerBot, new()
    {
        private T _instance;
        public InstanceResettablePokerBot()
        {
            _instance = new T();
        }
        public string Name => _instance.Name;
        public PokerAction GetAction(GameState state) => _instance.GetAction(state);
        public void Reset()
        {
            _instance = new T();
        }
    }
}
