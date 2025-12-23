using System;
using System.Threading;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class FoodGatheringTask : IBotTask
    {
        private AdbController _adb;
        private NavigationHelper _nav;
        
        // --- CONFIGURATION ---
        private int _maxQueues = 1; // Total Army Slots you have
        // ---------------------

        private DateTime _nextCheckTime = DateTime.MinValue;

        public FoodGatheringTask(AdbController adb)
        {
            _adb = adb;
            _nav = new NavigationHelper(adb);
        }

        public string Name => "Food Auto-Gather";
        public bool IsEnabled { get; set; } = true;
        // Priority 30: Lower than Fog (20), but higher than Daily VIP (50).
        public int Priority => 30; 

        public bool ShouldRun()
        {
            // 1. Throttle checks
            if (DateTime.Now < _nextCheckTime) return false;
            _nextCheckTime = DateTime.Now.AddSeconds(10);

            // 2. Count Active Marches (Gathering + Marching)
            using var screen = _adb.CaptureScreen();

            var gathering = VisionHelper.FindAllTemplates(screen, "assets/icon_status_gather.png", 0.85);
            var marching = VisionHelper.FindAllTemplates(screen, "assets/icon_status_march.png", 0.85);

            int busyQueues = gathering.Count + marching.Count;

            // Run if we have empty slots
            if (busyQueues < _maxQueues)
            {
                Logger.Log("Gather", $"Queues: {busyQueues}/{_maxQueues} busy. Sending harvesters...");
                return true;
            }

            return false;
        }

        public void Run()
        {
            // Loop until full
            while (true)
            {
                // 1. CHECK QUEUES (Re-check inside loop)
                using (var screenCheck = _adb.CaptureScreen())
                {
                    int g = VisionHelper.FindAllTemplates(screenCheck, "assets/icon_status_gather.png", 0.85).Count;
                    int m = VisionHelper.FindAllTemplates(screenCheck, "assets/icon_status_march.png", 0.85).Count;
                    if ((g + m) >= _maxQueues) 
                    {
                        Logger.Log("Gather", "All queues busy. Task complete.");
                        break;
                    }
                }

                // 2. ENSURE MAP VIEW
                if (!_nav.GoToMap())
                {
                    Logger.Log("Gather", "Could not reach Map.");
                    break;
                }
                
                Thread.Sleep(1500); // Wait for map load

                // 3. OPEN SEARCH MENU
                if (!ClickAndWait("assets/btn_search_map.png", "Search Menu"))
                {
                    // Maybe menu is already open? Try clicking Food directly.
                }

                // 4. SELECT FOOD
                // Note: We might need to reduce the Level slider if high level nodes are missing,
                // but for now we assume the default level is fine.
                if (!ClickAndWait("assets/icon_food.png", "Food Icon"))
                {
                    Logger.Log("Gather", "Food icon not found.");
                    _adb.SendBackKey();
                    break;
                }

                // 5. CLICK SEARCH
                if (!ClickAndWait("assets/btn_search_center.png", "Search Button"))
                {
                    Logger.Log("Gather", "Search button missing.");
                    _adb.SendBackKey();
                    break;
                }

                // Wait for camera to fly to the node
                Thread.Sleep(2500);

                // 7. CLICK GATHER BUTTON
                if (ClickAndWait("assets/btn_gather.png", "Gather Button"))
                {
                    // 8. CLICK NEW TROOPS (Optional)
                    // Sometimes the game auto-selects "New Troops". We check if we need to click it.
                    // We try to find it, if seen, click it. If not, maybe "March" is already there.
                    ClickAndWait("assets/btn_new_troops.png", "New Troops");

                    // 9. CLICK MARCH
                    if (ClickAndWait("assets/btn_march.png", "March"))
                    {
                        Logger.Log("Gather", "✅ March sent!");
                        Thread.Sleep(2000); // Wait for march to start
                    }
                    else
                    {
                        Logger.Log("Gather", "❌ March button blocked or missing.");
                        _adb.SendBackKey(); // Deselect node
                    }
                }
                else
                {
                    Logger.Log("Gather", "❌ Node gather button missing (Someone else taken?).");
                    // Zoom out or move slightly? 
                    // For now, just retry loop, the search will find another one.
                }
            }

            // Cleanup
            _nextCheckTime = DateTime.Now.AddSeconds(60); 
        }

        private bool ClickAndWait(string asset, string name)
        {
            using var screen = _adb.CaptureScreen();
            var loc = VisionHelper.FindTemplate(screen, asset, 0.8);
            if (loc != null)
            {
                _adb.Tap(loc.Value.X, loc.Value.Y);
                Thread.Sleep(1500); 
                return true;
            }
            return false;
        }
    }
}