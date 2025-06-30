namespace TournamentRunner.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minLevel;

        public ConsoleLogger(LogLevel minLevel = LogLevel.Info)
        {
            _minLevel = minLevel;
        }

        public void Log(LogLevel level, string message)
        {
            if (level <= _minLevel)
            {
                var prefix = level switch
                {
                    LogLevel.Error => "[ERROR] ",
                    LogLevel.Warning => "[WARN]  ",
                    LogLevel.Info => "[INFO]  ",
                    LogLevel.Debug => "[DEBUG] ",
                    LogLevel.Verbose => "[VERB]  ",
                    _ => ""
                };
                
                Console.WriteLine($"{prefix}{message}");
            }
        }

        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogVerbose(string message) => Log(LogLevel.Verbose, message);
    }
}
