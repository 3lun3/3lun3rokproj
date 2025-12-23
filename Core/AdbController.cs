using System;
using System.Linq;
using SharpAdbClient;
using System.Drawing; // For Bitmap
using System.IO;      // For Stream
using RoKBot.Helpers; // To access VisionHelper
using OpenCvSharp;    // For Mat
using System.Net; // For IPEndPoint
using System.Threading; // For CancellationToken


namespace RoKBot.Core
{
    public class AdbController
    {
        private readonly AdbClient _client;
        private DeviceData? _device;
        private readonly string _adbPath;

        public AdbController(string adbPath = @"C:\adb\platform-tools\adb.exe")
        {
            _adbPath = adbPath;
            _client = new AdbClient();
        }

        public void SendBackKey()
        {
            if (_device == null) return;
            // Keyevent 4 is the standard Android "Back" button
            _client.ExecuteRemoteCommand("input keyevent 4", _device, null);
            Console.WriteLine("[Action] Pressed BACK key");
        }

        public bool Connect()
        {
            try
            {
                var server = new AdbServer();
                server.StartServer(_adbPath, restartServerIfNewer: false);
                _client.Connect("127.0.0.1:5555");
                
                var devices = _client.GetDevices();
                if (devices.Count > 0)
                {
                    _device = devices[0];
                    Console.WriteLine($"[ADB] Connected to {_device.Name}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ADB] Error: {ex.Message}");
            }
            return false;
        }

        // Simulates a tap on the screen
        public void Tap(int x, int y)
        {
            if (_device == null) return;
            // ExecuteShellCommand is how we send events to Android
            _client.ExecuteRemoteCommand($"input tap {x} {y}", _device, null);
            Console.WriteLine($"[Action] Tapped at {x}, {y}");
        }

        // We will add Screenshot logic here later
        // Robust version: Takes a screenshot on the device and pulls the file
        public Mat CaptureScreen()
        {
            if (_device == null) return new Mat(); // Return empty if disconnected

            try
            {
                // Method 3: Base64 Stream (Slow but very reliable)
                // We ask android to take a pic, convert to base64, and print it.
                var receiver = new ConsoleOutputReceiver();
                _client.ExecuteRemoteCommand("screencap -p | base64", _device, receiver);

                var base64Data = receiver.ToString().Replace("\r\n", "");
                
                if (string.IsNullOrEmpty(base64Data)) return new Mat();

                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using (var stream = new MemoryStream(imageBytes))
                using (var bitmap = new Bitmap(stream))
                {
                    return VisionHelper.ToMat(bitmap).Clone();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Screen Error] {ex.Message}");
                return new Mat();
            }
        }
    }
}