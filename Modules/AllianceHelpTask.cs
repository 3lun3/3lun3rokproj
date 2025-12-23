using System;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class AllianceHelpTask : IBotTask
    {
        private AdbController _adb;

        public AllianceHelpTask(AdbController adb)
        {
            _adb = adb;
        }

        

        public string Name => "Alliance Auto-Help";
        public bool IsEnabled { get; set; } = true;
        
        // Priority 1 = Very High. We want to click this immediately when seen.
        public int Priority => 1; 

        // Add this variable
        private DateTime _nextCheck = DateTime.MinValue;

        public bool ShouldRun()
        {
            // Only check for helps every 3 seconds, not every 0.1 seconds
            return DateTime.Now >= _nextCheck;
        }

        public void Run()
        {
            // 1. Capture Screen
            using var screen = _adb.CaptureScreen();

            // 2. Look for the Help Icon
            // Lower threshold (0.7) because the hands animate/move slightly
            var location = VisionHelper.FindTemplate(screen, "assets/help_icon.png", 0.7);

            if (location != null)
            {
                Logger.Log("Alliance", "Help requested! Clicking...");
                
                // 3. Click the hands
                _adb.Tap(location.Value.X, location.Value.Y);
                
                // 4. Wait a tiny bit to let the animation finish
                System.Threading.Thread.Sleep(1000);
            }
            else
            {
                _nextCheck = DateTime.Now.AddSeconds(4);
            }
        }
    }
}