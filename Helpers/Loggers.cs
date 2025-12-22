using System;
using System.Collections.Generic;
using System.IO;

namespace RoKBot.Helpers
{
    public static class Logger
    {
        private static string _logPath = "bot_log.txt";
        // Store the last 10 messages for the UI
        public static List<string> History = new List<string>();
        public static event Action OnLog; // Let the UI know a new log arrived

        public static void Log(string module, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string uiMessage = $"[{timestamp}] [{module}] {message}";
            
            // 1. Add to History (Keep only last 10)
            History.Add(uiMessage);
            if (History.Count > 10) History.RemoveAt(0);

            // 2. Save to File
            try
            {
                File.AppendAllText(_logPath, uiMessage + Environment.NewLine);
            }
            catch { }

            // 3. Notify UI to redraw
            OnLog?.Invoke();
        }
    }
}