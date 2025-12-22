using System;
using RoKBot.Core;
using RoKBot.Helpers;
using OpenCvSharp;

namespace RoKBot.Modules
{
    public class TestTask : IBotTask
    {
        private AdbController _adb;
        
        // Pass the controller so we can use the camera
        public TestTask(AdbController adb)
        {
            _adb = adb;
        }

        public string Name => "Vision Test";
        public bool IsEnabled { get; set; } = true;
        public int Priority => 1;

        public bool ShouldRun() => true;

        public void Run()
        {
            // 1. Take a picture
            Console.WriteLine("Taking screenshot...");
            using var screen = _adb.CaptureScreen();

            // 2. Look for the avatar
            // MAKE SURE THIS PATH MATCHES YOUR FILE
            var location = VisionHelper.FindTemplate(screen, "assets/avatar.png", 0.85);

            if (location != null)
            {
                Console.WriteLine("✅ I SEE THE AVATAR! Testing click...");
                // 3. Optional: Click it (Don't do this if you are in a menu)
                // _adb.Tap(location.Value.X, location.Value.Y);
            }
            else
            {
                Console.WriteLine("❌ I do not see the target.");
            }
        }
    }
}