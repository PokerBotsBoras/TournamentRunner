// Runner/TournamentManager.cs
namespace TournamentRunner
{
    using TournamentRunner.Engine;
    using TournamentRunner.Logging;
    using System.Text.Json;

    public class RoundResult
    {
        public string BotA { get; set; } = "";
        public string BotB { get; set; } = "";
        public int BotAWins { get; set; }
        public int BotBWins { get; set; }
    }

    public class TournamentResults
    {
        public string Date { get; set; } = "";
        public List<TournamentRunner.RoundResult> Results { get; set; } = new();
    }
    public class TournamentManager
    {
        public void RunAllRounds(List<IResettablePokerBot> bots, int rounds, int handsPerRound)
        {
            Logger.LogInfo($"Running {rounds} rounds for {bots.Count} bots with {handsPerRound} hands each.");
            var results = new List<RoundResult>();
            var disqualified = new HashSet<string>();
            for (int i = 0; i < bots.Count; i++)
            {
                for (int j = 0; j < bots.Count; j++)
                {
                    if (i == j) continue;
                    var botA = bots[i];
                    var botB = bots[j];
                    Logger.LogDebug($"=== Starting round: {botA.Name} vs {botB.Name} ===");
                    if (disqualified.Contains(botA.Name) || disqualified.Contains(botB.Name))
                    {
                        Logger.LogDebug($"  Skipping round: {botA.Name} vs {botB.Name} (disqualified)");
                        continue;
                    }
                    int botAwins = 0;
                    int botBwins = 0;
                    bool disqualifiedInRound = false;
                    try
                    {
                        botA.Reset();
                        botB.Reset();
                        for (int m = 0; m < rounds; m++)
                        {
                            // if (m < 3 || m % 20 == 0)
                            //     Console.WriteLine($"  [Round {m + 1}/{rounds}] {botA.Name} vs {botB.Name}");
                            botA.Reset();
                            botB.Reset();
                            var engine = new PokerEngine();
                            int startingStack = 1000;
                            int botXStack = startingStack;
                            int botYStack = startingStack;
                            var botX = botA;
                            var botY = botB;
                            for (int h = 0; h < handsPerRound; h++)
                            {
                                if (botXStack < 10 || botYStack < 20)
                                    break;
                                // if (h < 3 || h % 10 == 0)
                                //     Console.WriteLine($"    [Hand {h + 1}/{handsPerRound}] {botX.Name} (SB) stack: {botXStack}, {botY.Name} (BB) stack: {botYStack}");
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
                                    disqualifiedInRound = true;
                                    break;
                                }
                            }
                            if (disqualifiedInRound)
                            {
                                Logger.LogWarning($"  Ending round early due to disqualification.");
                                break;
                            }
                            if (botXStack != botYStack)
                            {
                                var winner = botXStack > botYStack ? botX : botY;
                                // Console.WriteLine($"  [Round {m + 1}] Winner: {winner.Name}");
                                if (winner.Name == botA.Name)
                                    botAwins++;
                                else
                                    botBwins++;
                            }
                            else
                            {
                                Logger.LogDebug($"  [Round {m + 1}] Tie");
                            }
                        }
                        if (!disqualifiedInRound)
                        {
                            results.Add(new RoundResult
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
                        Logger.LogError($"Error running round between {botA.Name} and {botB.Name}: {ex.Message} {ex.StackTrace}");
                    }
                }
            }

            // Save results to file
            SaveResults(results);
        }

        private void SaveResults(List<RoundResult> results)
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
