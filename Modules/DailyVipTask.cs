using System;
using System.Threading;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class DailyVipTask : IBotTask
    {
        private AdbController _adb;
        private NavigationHelper _nav;
        private DateTime _lastRunTime = DateTime.MinValue;

        public DailyVipTask(AdbController adb)
        {
            _adb = adb;
            _nav = new NavigationHelper(adb);
        }

        public string Name => "Daily VIP Sequence";
        public bool IsEnabled { get; set; } = true;
        public int Priority => 50; 

        public bool ShouldRun()
        {
            // Run every 24h
            if ((DateTime.Now - _lastRunTime).TotalHours < 24) return false;
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

            // 5. Close VIP Menu (Click VIP Icon again, or use an X if you prefer)
            Logger.Log("DailyVIP", "Closing VIP Menu...");
            ClickAndWait("assets/vip_icon.png", "Close VIP"); // Using the icon to toggle it off

            // Success! Update timer.
            _lastRunTime = DateTime.Now;
            Logger.Log("DailyVIP", "Sequence complete.");
        }

        // --- Helper Functions for this Task ---

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
            // Rise of Kingdoms usually closes popups if you tap outside.
            // Let's try tapping coordinates (640, 100) - Top centerish
            _adb.Tap(640, 100);
            Thread.Sleep(1500);
        }
    }
}