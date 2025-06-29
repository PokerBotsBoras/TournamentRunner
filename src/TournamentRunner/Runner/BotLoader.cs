// Runner/BotLoader.cs
namespace TournamentRunner
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    public static class BotLoader
    {
        public static List<string> LoadExecutableBots(string root)
        {
            var botPaths = new List<string>();
            foreach (var dir in Directory.GetDirectories(root))
            {
                var exeDll = Directory.GetFiles(dir, "bot.dll")
                                      .FirstOrDefault();
                if (exeDll != null)
                    botPaths.Add(exeDll);
                else
                    Console.WriteLine($"No bot.dll in {dir}");
            }
            return botPaths;
        }

        public static List<IResettablePokerBot> LoadExternalResettableBots(List<string> botPaths)
        {
            var bots = new List<IResettablePokerBot>();
            foreach (var path in botPaths)
            {
                try
                {
                    bots.Add(new ExternalResettablePokerBot(new ExternalPokerBot(path)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load bot at {path}: {ex.Message}");
                }
            }
            return bots;
        }
    }

}
