using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RoKBot.Core;
using RoKBot.Helpers;

namespace RoKBot.Modules
{
    public class CavesExplorationTask : IBotTask
    {
        private AdbController _adb;
        private NavigationHelper _nav;
        private Random _rnd = new Random();

        // --- CONFIGURATION ---
        private int _maxScouts = 3; // Set this to your scout count
        // ---------------------

        private List<DateTime> _scoutReturnTimes;
        private DateTime _nextGlobalRunTime = DateTime.MinValue;

        // OCR Coordinates (Same as Fog)
        private const int OCR_X = -40;
        private const int OCR_Y = -57;
        private const int OCR_W = 80;
        private const int OCR_H = 24;

        private readonly string[] _campImages = { "assets/scout_camp_1.png", "assets/scout_camp_2.png" };

        public CavesExplorationTask(AdbController adb)
        {
            _adb = adb;
            _nav = new NavigationHelper(adb);
            
            _scoutReturnTimes = new List<DateTime>();
            for (int i = 0; i < _maxScouts; i++) _scoutReturnTimes.Add(DateTime.MinValue);
        }

        public string Name => "Cave Auto-Explorer";
        public bool IsEnabled { get; set; } = true;
        public int Priority => 25; // Slightly lower than Fog (20)

        public bool ShouldRun()
        {
            bool anyScoutFree = _scoutReturnTimes.Any(t => t <= DateTime.Now);
            return DateTime.Now >= _nextGlobalRunTime && anyScoutFree;
        }

       public void Run()
        {
            int scoutsSentThisLoop = 0;

            // Calculate how many scouts are ACTUALLY free right now
            int availableScouts = _scoutReturnTimes.Count(t => t <= DateTime.Now);
            if (availableScouts == 0) return;

            while (scoutsSentThisLoop < availableScouts)
            {
                // 1. GO TO CITY
                if (!_nav.GoToCity())
                {
                    _adb.SendBackKey();
                    Thread.Sleep(1000);
                    if (!_nav.GoToCity()) return; 
                }

                Thread.Sleep(2000); 

                // 2. FIND CAMP
                using var screen1 = _adb.CaptureScreen();
                var campLoc = VisionHelper.FindAny(screen1, _campImages, 0.65);

                if (campLoc == null) {
                    Logger.Log("Caves", "❌ Scout Camp not found.");
                    break; 
                }

                _adb.Tap(campLoc.Value.X, campLoc.Value.Y);
                Thread.Sleep(1200);

                // 3. SPYGLASS
                if (!ClickAndWait("assets/spyglass.png", "Spyglass")) 
                {
                    _adb.SendBackKey(); 
                    break;
                }

                // 4. CLICK CAVES TAB
                if (!ClickAndWait("assets/tab_caves.png", "Caves Tab"))
                {
                     Logger.Log("Caves", "❌ Caves tab not found.");
                     _adb.SendBackKey();
                     break;
                }
                
                // 5. FIND ALL "IR" BUTTONS
                using var screenMenu = _adb.CaptureScreen();
                // Ensure you have the VisionHelper update I gave you previously (Duplicate Removal)!
                var buttons = VisionHelper.FindAllTemplates(screenMenu, "assets/btn_go_cave.png", 0.9);
                
                if (buttons.Count == 0)
                {
                    Logger.Log("Caves", "⚠️ No 'Ir' buttons found.");
                    _adb.SendBackKey();
                    UpdateFreeSlotsToBusyFallback();
                    break;
                }

                // --- LOGIC CHANGE: BOTTOM UP ---
                // If we found 3 buttons (Count=3).
                // 1st pass (0 sent) -> Index = (3-1) - 0 = 2 (The Bottom one).
                // 2nd pass (1 sent) -> Index = (3-1) - 1 = 1 (The Middle one).
                int buttonIndex = (buttons.Count - 1) - scoutsSentThisLoop;
                
                // Safety Check: If the math goes negative (unexpected), reset to 0
                if (buttonIndex < 0) buttonIndex = 0;

                var targetButton = buttons[buttonIndex];
                Logger.Log("Caves", $"Clicking 'Ir' button #{buttonIndex + 1} (Y:{targetButton.Y})");
                
                _adb.Tap(targetButton.X, targetButton.Y);
                Thread.Sleep(2500); 

                // 7. CLICK INVESTIGATE
                if (ClickAndWait("assets/btn_investigate.png", "Investigate"))
                {
                    // 8. CLICK SEND & OCR
                    using var screenSend = _adb.CaptureScreen();
                    var sendBtnLoc = VisionHelper.FindTemplate(screenSend, "assets/btn_send.png", 0.8);

                    if (sendBtnLoc != null)
                    {
                        TimeSpan duration = TimeSpan.FromMinutes(5); 

                        // OCR Logic
                        int rx = sendBtnLoc.Value.X + OCR_X;
                        int ry = sendBtnLoc.Value.Y + OCR_Y;
                        if (rx < 0) rx = 0; if (ry < 0) ry = 0;

                        var region = new OpenCvSharp.Rect(rx, ry, OCR_W, OCR_H);
                        string timeText = OcrHelper.ReadTextFromImage(screenSend, region);
                        var parsed = OcrHelper.ParseTime(timeText);
                        
                        if (parsed.TotalSeconds > 0) duration = parsed;

                        _adb.Tap(sendBtnLoc.Value.X, sendBtnLoc.Value.Y);
                        Logger.Log("Caves", $"✅ Scout sent. Duration: {duration}");
                        Thread.Sleep(1500);

                        // Update Memory (Duration + 15s Margin)
                        double totalSeconds = duration.TotalSeconds + 15;
                        DateTime returnTime = DateTime.Now.AddSeconds(totalSeconds);
                        UpdateFirstFreeSlot(returnTime);
                        
                        scoutsSentThisLoop++;
                    }
                    else 
                    {
                        Logger.Log("Caves", "❌ 'Send' button missing.");
                        break; 
                    }
                }
                else
                {
                    Logger.Log("Caves", "❌ 'Investigate' button not found.");
                    break;
                }
            }
            
            _nav.GoToCity();
            
            DateTime nextFree = _scoutReturnTimes.Min();
            if (nextFree < DateTime.Now) nextFree = DateTime.Now.AddSeconds(10);
            _nextGlobalRunTime = nextFree;
        }
        private void UpdateFirstFreeSlot(DateTime newReturnTime)
        {
            // Update the first slot that is free
            for (int i = 0; i < _scoutReturnTimes.Count; i++)
            {
                if (_scoutReturnTimes[i] <= DateTime.Now)
                {
                    _scoutReturnTimes[i] = newReturnTime;
                    return;
                }
            }
            // Fallback
            var soonestIndex = _scoutReturnTimes.IndexOf(_scoutReturnTimes.Min());
            _scoutReturnTimes[soonestIndex] = newReturnTime;
        }

        private void UpdateFreeSlotsToBusyFallback()
        {
            for (int i = 0; i < _scoutReturnTimes.Count; i++)
            {
                if (_scoutReturnTimes[i] <= DateTime.Now) _scoutReturnTimes[i] = DateTime.Now.AddMinutes(5);
            }
            _nextGlobalRunTime = _scoutReturnTimes.Min();
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