using System;
using System.Threading;
using RoKBot.Core;

namespace RoKBot.Helpers
{
    public class NavigationHelper
    {
        private AdbController _adb;

        public NavigationHelper(AdbController adb)
        {
            _adb = adb;
        }

        // Returns true if we are definitely inside the City
        public bool IsInCity()
        {
            using var screen = _adb.CaptureScreen();
            // If we see the "Map" button, we are in the City
            return VisionHelper.FindTemplate(screen, "assets/map.png", 0.8) != null;
        }

        // Returns true if we are definitely on the World Map
        public bool IsInMap()
        {
            using var screen = _adb.CaptureScreen();
            // If we see the "City" button, we are on the Map
            return VisionHelper.FindTemplate(screen, "assets/city.png", 0.8) != null;
        }

        public bool GoToCity()
        {
            if (IsInCity()) return true;

            Logger.Log("Navigation", "Moving to City...");
            
            // 1. Find the button to go home
            using var screen = _adb.CaptureScreen();
            var loc = VisionHelper.FindTemplate(screen, "assets/city.png", 0.8);
            
            if (loc != null)
            {
                _adb.Tap(loc.Value.X, loc.Value.Y);
                Thread.Sleep(3000); // Wait for load
                
                // Double check
                if (IsInCity()) return true;
            }
            
            Logger.Log("Navigation", "Failed to reach City.");
            return false;
        }

        public bool GoToMap()
        {
            if (IsInMap()) return true;

            Logger.Log("Navigation", "Moving to World Map...");

            using var screen = _adb.CaptureScreen();
            var loc = VisionHelper.FindTemplate(screen, "assets/map.png", 0.8);

            if (loc != null)
            {
                _adb.Tap(loc.Value.X, loc.Value.Y);
                Thread.Sleep(3000); // Wait for load

                if (IsInMap()) return true;
            }

            Logger.Log("Navigation", "Failed to reach Map.");
            return false;
        }
    }
}