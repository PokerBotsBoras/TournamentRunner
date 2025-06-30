// Runner/TournamentManager.cs
namespace TournamentRunner
{
    using TournamentRunner.Engine;
    using TournamentRunner.Logging;
    using System.Text.Json;

    public class MatchResult
    {
        public string BotA { get; set; } = "";
        public string BotB { get; set; } = "";
        public int BotAWins { get; set; }
        public int BotBWins { get; set; }
    }

    public class TournamentResults
    {
        public string Date { get; set; } = "";
        public List<TournamentRunner.MatchResult> Results { get; set; } = new();
    }
    public class TournamentManager
    {
        public void RunAllMatches(List<IResettablePokerBot> bots, int matches, int handsPerMatch)
        {
            Logger.LogInfo($"Running {matches} matches for {bots.Count} bots with {handsPerMatch} hands each.");
            var results = new List<MatchResult>();
            var disqualified = new HashSet<string>();
            for (int i = 0; i < bots.Count; i++)
            {
                for (int j = 0; j < bots.Count; j++)
                {
                    if (i == j) continue;
                    var botA = bots[i];
                    var botB = bots[j];
                    Logger.LogDebug($"=== Starting match: {botA.Name} vs {botB.Name} ===");
                    if (disqualified.Contains(botA.Name) || disqualified.Contains(botB.Name))
                    {
                        Logger.LogDebug($"  Skipping match: {botA.Name} vs {botB.Name} (disqualified)");
                        continue;
                    }
                    int botAwins = 0;
                    int botBwins = 0;
                    bool disqualifiedInMatch = false;
                    try
                    {
                        botA.Reset();
                        botB.Reset();
                        for (int m = 0; m < matches; m++)
                        {
                            // if (m < 3 || m % 20 == 0)
                            //     Console.WriteLine($"  [Match {m + 1}/{matches}] {botA.Name} vs {botB.Name}");
                            botA.Reset();
                            botB.Reset();
                            var engine = new PokerEngine();
                            int startingStack = 1000;
                            int botXStack = startingStack;
                            int botYStack = startingStack;
                            var botX = botA;
                            var botY = botB;
                            for (int h = 0; h < handsPerMatch; h++)
                            {
                                if (botXStack < 10 || botYStack < 20)
                                    break;
                                // if (h < 3 || h % 10 == 0)
                                //     Console.WriteLine($"    [Hand {h + 1}/{handsPerMatch}] {botX.Name} (SB) stack: {botXStack}, {botY.Name} (BB) stack: {botYStack}");
                                try
                                {
                                    var result = engine.PlayHand(botX, botY, botXStack, botYStack);
                                    botXStack = result.BotAStack;
                                    botYStack = result.BotBStack;
                                    (botX, botY) = (botY, botX);
                                    (botXStack, botYStack) = (botYStack, botXStack);
                                    // if (h < 3 || h % 10 == 0)
                                    //     Console.WriteLine($"    [Hand {h + 1}] End stacks: {botA.Name}: {botXStack}, {botB.Name}: {botYStack}");
                                    if (botXStack <= 0 || botYStack <= 0)
                                        break;
                                }
                                catch (BotException ex)
                                {
                                    Logger.LogWarning($"    Bot '{ex.BotName}' disqualified during hand {h + 1}: {ex.Inner.Message}");
                                    disqualified.Add(ex.BotName);
                                    disqualifiedInMatch = true;
                                    break;
                                }
                            }
                            if (disqualifiedInMatch)
                            {
                                Logger.LogWarning($"  Ending match early due to disqualification.");
                                break;
                            }
                            if (botXStack != botYStack)
                            {
                                var winner = botXStack > botYStack ? botX : botY;
                                // Console.WriteLine($"  [Match {m + 1}] Winner: {winner.Name}");
                                if (winner.Name == botA.Name)
                                    botAwins++;
                                else
                                    botBwins++;
                            }
                            else
                            {
                                Logger.LogDebug($"  [Match {m + 1}] Tie");
                            }
                        }
                        if (!disqualifiedInMatch)
                        {
                            results.Add(new MatchResult
                            {
                                BotA = botA.Name,
                                BotB = botB.Name,
                                BotAWins = botAwins,
                                BotBWins = botBwins
                            });
                            Logger.LogInfo($"=== {botAwins} {botA.Name} - {botB.Name} {botBwins} ===");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error running match between {botA.Name} and {botB.Name}: {ex.Message} {ex.StackTrace}");
                    }
                }
            }

            // Save results to file
            SaveResults(results);
        }

        private void SaveResults(List<MatchResult> results)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var fileName = $"results_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json";
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(new TournamentResults { Date = date, Results = results }, options);
            File.WriteAllText(fileName, json);
            File.WriteAllText("results.json", json);
            Logger.LogInfo($"Results saved to {fileName} and results.json");
        }
    }
}
