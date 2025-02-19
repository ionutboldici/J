using System;
using System.IO;
using System.Threading;

namespace J100.Services
{
    public static class erl
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        private static readonly string BackupLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_old.log");
        private static readonly object LockObject = new object();
        private static readonly int MaxLogSize = 1024 * 1024; // 1MB

        // Instanțierea ConfigReader pentru a apela GetValue într-un context non-static
        private static readonly ConfigReader configReader = new ConfigReader("config.ini");
        private static readonly bool LoggingEnabled = configReader.GetValue("General", "enable_logging", "true").ToLower() == "true";

        public static void LogError(string message)
        {
            Log(message, "ERROR");
        }

        public static void LogWarning(string message)
        {
            Log(message, "WARNING");
        }

        public static void LogInfo(string message)
        {
            Log(message, "INFO");
        }

        private static void Log(string message, string severity)
        {
            if (!LoggingEnabled) return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [{severity}] {message}";

            lock (LockObject)
            {
                try
                {
                    if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogSize)
                    {
                        if (File.Exists(BackupLogFilePath))
                        {
                            File.Delete(BackupLogFilePath);
                        }
                        File.Move(LogFilePath, BackupLogFilePath);
                    }

                    int retryCount = 3;
                    while (retryCount > 0)
                    {
                        try
                        {
                            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                            break;
                        }
                        catch (IOException)
                        {
                            retryCount--;
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FATAL ERROR] Nu s-a putut scrie în log: {ex.Message}");
                }
            }
        }
    }
}
