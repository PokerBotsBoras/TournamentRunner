namespace TournamentRunner.Logging
{
    public enum LogLevel
    {
        Silent = 0,     // No output
        Error = 1,      // Only errors
        Warning = 2,    // Errors and warnings
        Info = 3,       // Basic information (default)
        Debug = 4,      // Detailed information
        Verbose = 5     // Full poker engine details
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message);
        void LogError(string message);
        void LogWarning(string message);
        void LogInfo(string message);
        void LogDebug(string message);
        void LogVerbose(string message);
    }
}
