using System;
using System.IO; // Required for saving/loading memory
using System.Threading;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class DailyVipTask : IBotTask
    {
        private AdbController _adb;
        private NavigationHelper _nav;
        
        // Memory System
        private DateTime _lastRunTime = DateTime.MinValue;
        private readonly string _configPath = "vip_last_run.txt";

        public DailyVipTask(AdbController adb)
        {
            _adb = adb;
            _nav = new NavigationHelper(adb);

            // LOAD MEMORY: Check if we have a file with the last date
            if (File.Exists(_configPath))
            {
                string savedDate = File.ReadAllText(_configPath);
                // Try to parse the text back into a DateTime object
                DateTime.TryParse(savedDate, out _lastRunTime);
            }
        }

        public string Name => "Daily VIP Sequence";
        public bool IsEnabled { get; set; } = true;
        public int Priority => 50; 

        public bool ShouldRun()
        {
            // If we ran today (same Day and Year), don't run again
            if (_lastRunTime.Date == DateTime.Today) return false;
            return true;
        }

        public void Run()
        {
            Logger.Log("DailyVIP", "Starting sequence...");

            // 1. Ensure we are in the City
            if (!_nav.GoToCity()) return;

            // 2. Open VIP Menu (Click VIP Icon)
            if (!ClickAndWait("assets/vip_icon.png", "VIP Icon")) return;

            // 3. Click Chest (First Reward)
            if (ClickAndWait("assets/vip_chest.png", "VIP Chest"))
            {
                ClosePopup(); // Handle the reward popup
            }

            // 4. Click Claim Button (Second Reward)
            if (ClickAndWait("assets/reclamar_button.png", "Claim Button"))
            {
                ClosePopup(); // Handle the reward popup
            }

            // 5. Close VIP Menu (Click VIP Icon again to toggle off)
            Logger.Log("DailyVIP", "Closing VIP Menu...");
            // We use the icon again because in RoK clicking the VIP icon usually closes the window too.
            // If this fails, use a specific "close_x.png" or the ClosePopup method.
            ClickAndWait("assets/vip_icon.png", "Close VIP"); 

            // SUCCESS! SAVE MEMORY:
            _lastRunTime = DateTime.Now;
            File.WriteAllText(_configPath, _lastRunTime.ToString());
            Logger.Log("DailyVIP", "Sequence complete. Saved to file.");
        }

        // --- Helper Functions ---

        // Tries to find an image, click it, and wait. Returns false if not found.
        private bool ClickAndWait(string asset, string stepName)
        {
            using var screen = _adb.CaptureScreen();
            var loc = VisionHelper.FindTemplate(screen, asset, 0.8);

            if (loc != null)
            {
                Logger.Log("DailyVIP", $"Step: {stepName}");
                _adb.Tap(loc.Value.X, loc.Value.Y);
                Thread.Sleep(2500); // Wait for UI animation
                return true;
            }
            
            Logger.Log("DailyVIP", $"Skipping: Could not find {stepName}");
            return false;
        }

        // Simulates closing a popup window
        private void ClosePopup()
        {
            Logger.Log("DailyVIP", "Closing Popup...");
            
            // OPTION A: If you have an X button asset
            // ClickAndWait("assets/close_x.png", "Close X");

            // OPTION B: Tap a "Safe Spot" (e.g., top center) to dismiss
            // Rise of Kingdoms usually closes popups if you tap outside the box.
            _adb.Tap(640, 100);
            Thread.Sleep(1500);
        }
    }
}