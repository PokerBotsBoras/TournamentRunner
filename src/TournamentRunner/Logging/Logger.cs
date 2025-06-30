namespace TournamentRunner.Logging
{
    public static class Logger
    {
        private static ILogger _instance = new ConsoleLogger();

        public static void Configure(ILogger logger)
        {
            _instance = logger;
        }

        public static void Log(LogLevel level, string message) => _instance.Log(level, message);
        public static void LogError(string message) => _instance.LogError(message);
        public static void LogWarning(string message) => _instance.LogWarning(message);
        public static void LogInfo(string message) => _instance.LogInfo(message);
        public static void LogDebug(string message) => _instance.LogDebug(message);
        public static void LogVerbose(string message) => _instance.LogVerbose(message);
    }
}
