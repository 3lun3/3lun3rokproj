using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoKBot.Core;
using RoKBot.Helpers;
using RoKBot.Modules;

class Program
{
    // Global State
    static bool IsRunning = false; // Start paused
    static List<IBotTask> Tasks = new List<IBotTask>();
    static AdbController Adb;

    static void Main(string[] args)
    {
        Console.CursorVisible = false;
        Console.Title = "RoK Professional Bot v1.0";
        
        // 1. Initialize Hardware
        Logger.Log("System", "Connecting to ADB...");
        Adb = new AdbController();
        if (!Adb.Connect()) 
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // 2. Load Modules
        Tasks.Add(new AllianceHelpTask(Adb));
        // Tasks.Add(new TrainingTask(Adb)); // We will add this later
        // 2. Load Modules
        Tasks.Add(new DailyVipTask(Adb)); // Vip chest

        // 3. Start the Bot Logic in a BACKGROUND Thread
        Task.Run(() => BotLoop());

        // 4. Start the UI Loop in the MAIN Thread
        // Hook into the Logger so the screen updates when a log happens
        Logger.OnLog += DrawUI; 
        
        DrawUI(); // Initial draw

        while (true)
        {
            // Listen for key presses without blocking the bot
            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.Spacebar:
                    IsRunning = !IsRunning;
                    Logger.Log("System", IsRunning ? "RESUMED" : "PAUSED");
                    break;

                case ConsoleKey.D1: // Key '1'
                case ConsoleKey.NumPad1:
                    ToggleTask(0);
                    break;

                case ConsoleKey.D2: // Key '2'
                case ConsoleKey.NumPad2:
                    ToggleTask(1);
                    break;
                
                // Add more keys as we add modules
            }
            DrawUI();
        }
    }

    // --- THE UI RENDERER ---
    static void DrawUI()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================");
        Console.WriteLine("       RISE OF KINGDOMS BOT - DASHBOARD   ");
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
            Console.WriteLine($"  [{i + 1}] {status} {t.Name}");
        }
        Console.ResetColor();
        Console.WriteLine("------------------------------------------");

        // 3. Log History
        Console.WriteLine("RECENT LOGS:");
        foreach (var log in Logger.History)
        {
            Console.WriteLine("  " + log);
        }
    }

    static void ToggleTask(int index)
    {
        if (index >= 0 && index < Tasks.Count)
        {
            Tasks[index].IsEnabled = !Tasks[index].IsEnabled;
            // Force redraw immediately
            DrawUI(); 
        }
    }

    // --- THE BOT LOGIC LOOP ---
    static void BotLoop()
    {
        while (true)
        {
            // If Paused, just wait and do nothing
            if (!IsRunning) 
            {
                Thread.Sleep(500);
                continue;
            }

            try
            {
                // Run enabled tasks by priority
                var activeTasks = Tasks.Where(t => t.IsEnabled).OrderBy(t => t.Priority).ToList();

                foreach (var task in activeTasks)
                {
                    // Only run if the task says it's ready
                    if (task.ShouldRun())
                    {
                        task.Run();
                        // Small break between tasks
                        Thread.Sleep(500);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error", $"Crash in BotLoop: {ex.Message}");
            }

            // Heartbeat
            Thread.Sleep(1000);
        }
    }
}