using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoKBot.Core;
using RoKBot.Helpers;
using RoKBot.Modules;

namespace RoKBot
{
    class Program
    {
        // Global State
        static bool IsRunning = false; // Starts PAUSED
        static List<IBotTask> Tasks = new List<IBotTask>();
        static AdbController Adb;

        static void Main(string[] args)
        {
            // Setup Console Window
            Console.CursorVisible = false;
            Console.Title = "RoK Professional Bot v1.2";

            // 1. Initialize Hardware (ADB Connection)
            Logger.Log("System", "Connecting to ADB...");
            Adb = new AdbController();
            
            if (!Adb.Connect()) 
            {
                Logger.Log("System", "❌ ADB Connection Failed. Exiting...");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // 2. Load Modules
            // The order here determines the Button Number (1, 2, 3...)
            Tasks.Add(new AllianceHelpTask(Adb));     // Priority 1 (High)
            Tasks.Add(new FogExplorationTask(Adb));   // Priority 20 (Medium)
            Tasks.Add(new DailyVipTask(Adb));         // Priority 5 (Low/Once per day)
            Tasks.Add(new CavesExplorationTask(Adb)); // 30 (Medium)
            Tasks.Add(new FoodGatheringTask(Adb));

            Logger.Log("System", $"Loaded {Tasks.Count} modules.");

            // 3. Start the Bot Logic in a BACKGROUND Thread
            // This ensures the bot working doesn't freeze your ability to press keys
            Task.Run(() => BotLoop());

            // 4. Hook into Logger to redraw UI whenever a new log arrives
            Logger.OnLog += DrawUI;
            
            // Initial Draw
            DrawUI();

            // 5. Main UI Loop (Listens for Key Presses)
            while (true)
            {
                // ReadKey(true) intercepts the key so it doesn't print to screen
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;

                // Toggle Paused/Running
                if (key == ConsoleKey.Spacebar)
                {
                    IsRunning = !IsRunning;
                    Logger.Log("System", IsRunning ? "RESUMED [Bot is Running]" : "PAUSED [Bot Stopped]");
                }
                // Handle Number Keys (1-9) for toggling modules
                else if (key >= ConsoleKey.D1 && key <= ConsoleKey.D9)
                {
                    int index = key - ConsoleKey.D1; // '1' becomes index 0
                    ToggleTask(index);
                }
                // Handle Numpad Keys (1-9)
                else if (key >= ConsoleKey.NumPad1 && key <= ConsoleKey.NumPad9)
                {
                    int index = key - ConsoleKey.NumPad1;
                    ToggleTask(index);
                }

                // Force a redraw after any key press
                DrawUI();
            }
        }

        // --- UI RENDERER ---
        // Draws the "Hacker Dashboard"
        static void DrawUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================");
            Console.WriteLine("       RISE OF KINGDOMS BOT - CONTROL     ");
            Console.WriteLine("==========================================");
            Console.ResetColor();

            // 1. Status Bar
            Console.Write("STATUS: ");
            if (IsRunning)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ACTIVE [RUNNING]");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("STOPPED [PAUSED] (Press SPACE to Start)");
            }
            Console.ResetColor();
            Console.WriteLine("------------------------------------------");

            // 2. Modules List
            Console.WriteLine("MODULES (Press Number to Toggle):");
            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                string status = t.IsEnabled ? "[ON] " : "[OFF]";
                ConsoleColor color = t.IsEnabled ? ConsoleColor.Green : ConsoleColor.Gray;

                Console.ForegroundColor = color;
                // Output: [1] [ON] Alliance Auto-Help
                Console.WriteLine($"  [{i + 1}] {status} {t.Name}");
            }
            Console.ResetColor();
            Console.WriteLine("------------------------------------------");

            // 3. Log History
            Console.WriteLine("RECENT LOGS:");
            // We use a copy of the list to avoid errors if the list changes while drawing
            var logs = Logger.History.ToList(); 
            foreach (var log in logs)
            {
                Console.WriteLine("  " + log);
            }
        }

        // Helper to safely toggle a task
        static void ToggleTask(int index)
        {
            if (index >= 0 && index < Tasks.Count)
            {
                var task = Tasks[index];
                task.IsEnabled = !task.IsEnabled;
                Logger.Log("System", $"Toggled {task.Name}: {(task.IsEnabled ? "ON" : "OFF")}");
            }
        }

        // --- BOT LOGIC ENGINE ---
        // This runs in the background forever
        static void BotLoop()
    {
        while (true)
        {
            if (!IsRunning) 
            {
                Thread.Sleep(500);
                continue;
            }

            try
            {
                // 1. Get all enabled tasks sorted by Priority (1 is highest)
                var activeTasks = Tasks.Where(t => t.IsEnabled).OrderBy(t => t.Priority).ToList();
                
                bool didWork = false;

                foreach (var task in activeTasks)
                {
                    // 2. Check if this task is ready to run
                    if (task.ShouldRun())
                    {
                        // 3. LOGIC CHANGE: Run ONLY the highest priority task
                        Logger.Log("Core", $"Running Task: {task.Name}");
                        task.Run();
                        
                        didWork = true;
                        
                        // 4. BREAK! Stop looking at lower priorities. 
                        // We restart the loop immediately to see if a High Priority task 
                        // (like Alliance Help) became ready while we were working.
                        break; 
                    }
                }

                // If no task was ready (everyone is waiting/sleeping), take a nap
                if (!didWork)
                {
                    Thread.Sleep(1000); // 1 second idle wait
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error", $"Crash in BotLoop: {ex.Message}");
                Thread.Sleep(3000);
            }
        }
    }
    }
}