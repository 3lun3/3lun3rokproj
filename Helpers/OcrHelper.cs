using System;
using System.IO;
using OpenCvSharp;
using Tesseract;

namespace RoKBot.Helpers
{
    public static class OcrHelper
    {
        private static TesseractEngine _engine;

        public static void Initialize()
        {
            if (_engine == null)
            {
                _engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.LstmOnly);
                
                // 1. Force Single Line Mode (Crucial for timers)
                _engine.DefaultPageSegMode = PageSegMode.SingleLine;
                
                // 2. Allow numbers and colons only
                _engine.SetVariable("tessedit_char_whitelist", "0123456789:");
                _engine.SetVariable("debug_file", "/dev/null");
            }
        }

        public static string ReadTextFromImage(Mat screen, OpenCvSharp.Rect region)
        {
            Initialize();

            // 1. Crop
            using var cropped = new Mat(screen, region);

            // 2. GRAYSCALE
            using var gray = new Mat();
            Cv2.CvtColor(cropped, gray, ColorConversionCodes.BGR2GRAY);

            // 3. UPSCALE (The Fix for Pixel Fonts)
            // We verify the size. If it's tiny (height < 30), we scale it up 3x.
            using var scaled = new Mat();
            if (gray.Height < 50)
            {
                Cv2.Resize(gray, scaled, new OpenCvSharp.Size(0, 0), 3.0, 3.0, InterpolationFlags.Cubic);
            }
            else
            {
                gray.CopyTo(scaled);
            }

            // 4. THRESHOLD (Binarization)
            // Invert: Make it Black text on White background (Tesseract prefers this)
            // If your raw image is White text on Dark background, use ThresholdTypes.Binary
            // If the debug image you showed me is the result of this step, 
            // ensure the final result is BLACK text on WHITE background.
            using var binary = new Mat();
            Cv2.Threshold(scaled, binary, 150, 255, ThresholdTypes.BinaryInv); 

            // 5. ADD BORDER (Padding)
            // Tesseract hates text touching the edge. Add 10px white border.
            using var padded = new Mat();
            Cv2.CopyMakeBorder(binary, padded, 10, 10, 10, 10, BorderTypes.Constant, Scalar.White);

            // --- DEBUG: Save to see the result ---
            padded.SaveImage("ocr_debug.png"); 
            // ------------------------------------

            string tempFile = "ocr_temp.png"; 
            padded.SaveImage(tempFile);

            try 
            {
                using var img = Pix.LoadFromFile(tempFile);
                using var page = _engine.Process(img);
                var text = page.GetText().Trim();
                
                // Debug log
                // Console.WriteLine($"[OCR RAW] '{text}'");
                return text;
            }
            finally
            {
               if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
        
        public static TimeSpan ParseTime(string text)
        {
            try 
            {
                // Clean common pixel-art noise
                // Sometimes '0' is read as 'O' or 'D'
                text = text.Replace("O", "0")
                           .Replace("o", "0")
                           .Replace("D", "0")
                           .Replace(" ", ""); // Remove spaces
                
                if (TimeSpan.TryParse(text, out var result)) return result;
            }
            catch { }
            return TimeSpan.Zero;
        }
    }
}