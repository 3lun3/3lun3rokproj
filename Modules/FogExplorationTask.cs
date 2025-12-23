using System;
using System.Collections.Generic;
using System.Threading;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class FogExplorationTask : IBotTask
    {
        private AdbController _adb;
        private NavigationHelper _nav;
        
        // --- CONFIGURATION ---
        private int _maxScouts = 2; // Set to your scout count
        // ---------------------

        private DateTime _nextCheckTime = DateTime.MinValue;

        // Assets
        private readonly string[] _campImages = { "assets/scout_camp_1.png", "assets/scout_camp_2.png" };

        public FogExplorationTask(AdbController adb)
        {
            _adb = adb;
            _nav = new NavigationHelper(adb);
        }

        public string Name => "Fog Auto-Explorer";
        public bool IsEnabled { get; set; } = true;
        public int Priority => 20; 

        public bool ShouldRun()
        {
            // 1. Throttle: Only check screen every 5 seconds to save CPU
            if (DateTime.Now < _nextCheckTime) return false;
            _nextCheckTime = DateTime.Now.AddSeconds(5);

            // 2. State Check
            // We need to be in the City or Map to see the icons. 
            // If we are in a menu, we can't tell, so we assume false to let Navigation handle it later.
            // But for now, we just take a peek.
            using var screen = _adb.CaptureScreen();

            // Count Active (Purple)
            var purpleIcons = VisionHelper.FindAllTemplates(screen, "assets/icon_march_purple.png", 0.85);
            int activeScouts = purpleIcons.Count;

            // Count Idle Camping (Blue)
            var blueIcons = VisionHelper.FindAllTemplates(screen, "assets/icon_march_blue.png", 0.85);
            int campingScouts = blueIcons.Count;

            // Logic: 
            // Run if we have fewer active scouts than our max.
            // OR if we have anyone camping (Blue) who should be working.
            bool needToSend = (activeScouts < _maxScouts) || (campingScouts > 0);

            if (needToSend)
            {
                Logger.Log("Fog", $"State: {activeScouts} Active, {campingScouts} Camping. Need to send.");
                return true;
            }

            return false;
        }

        public void Run()
        {
            // 1. Double check count to calculate how many we need to send
            int scoutsToSend = 0;
            using (var screenCheck = _adb.CaptureScreen())
            {
                int active = VisionHelper.FindAllTemplates(screenCheck, "assets/icon_march_purple.png", 0.85).Count;
                scoutsToSend = _maxScouts - active;
            }

            if (scoutsToSend <= 0) return;

            Logger.Log("Fog", $"Starting cycle to send {scoutsToSend} scout(s)...");

            int sentCount = 0;

            // Loop until we filled the slots
            while (sentCount < scoutsToSend)
            {
                // ALWAYS GO TO CITY START OF LOOP
                if (!_nav.GoToCity())
                {
                    _adb.SendBackKey();
                    Thread.Sleep(1000);
                    if (!_nav.GoToCity()) return; 
                }

                Thread.Sleep(2000); 

                // FIND CAMP
                using var screen1 = _adb.CaptureScreen();
                var campLoc = VisionHelper.FindAny(screen1, _campImages, 0.65);

                if (campLoc == null) {
                    Logger.Log("Fog", "❌ Scout Camp not found.");
                    break; 
                }

                _adb.Tap(campLoc.Value.X, campLoc.Value.Y);
                Thread.Sleep(1200);

                // SPYGLASS
                if (!ClickAndWait("assets/spyglass.png", "Spyglass")) 
                {
                    _adb.SendBackKey(); 
                    break;
                }

                // IDLE CHECK (Game Menu)
                // If the game says "0 Idle", we stop immediately, even if our icon count said otherwise.
                bool foundButton = ClickAndWait("assets/btn_explore_1.png", "Explore (Menu)");

                if (!foundButton)
                {
                    Logger.Log("Fog", "⚠️ Game says scouts busy (Wall hit). Stopping.");
                    _adb.SendBackKey();
                    Thread.Sleep(1000);
                    break; 
                }

                // EXPLORE TARGET
                if (ClickAndWait("assets/btn_explore_2.png", "Explore (Target)"))
                {
                    // SEND (No OCR needed anymore!)
                    // We just look for the Send button and click it.
                    if (ClickAndWait("assets/btn_send.png", "Send Scout"))
                    {
                         Logger.Log("Fog", "✅ Scout sent.");
                         sentCount++;
                    }
                    else 
                    {
                        Logger.Log("Fog", "❌ 'Send' button missing.");
                        break; 
                    }
                }
                else
                {
                    Logger.Log("Fog", "❌ Explore target missing.");
                    break;
                }
            }
            
            // Cleanup: Go home and wait a bit before checking icons again
            _nav.GoToCity();
            _nextCheckTime = DateTime.Now.AddSeconds(10); 
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