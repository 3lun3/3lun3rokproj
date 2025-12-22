using System;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace RoKBot.Helpers
{
    public static class VisionHelper
    {
        public static Mat ToMat(Bitmap bitmap)
        {
            return BitmapConverter.ToMat(bitmap);
        }

        public static System.Drawing.Point? FindTemplate(Mat screen, string templatePath, double threshold = 0.8)
        {
            // SAFETY CHECK 1: Did we get a valid screen?
            if (screen.Empty())
            {
                Console.WriteLine("[Vision] Warning: Screen image is empty. Skipping frame.");
                return null;
            }

            // 1. Load the template
            using var rawTemplate = Cv2.ImRead(templatePath, ImreadModes.Color);
            if (rawTemplate.Empty())
            {
                Console.WriteLine($"[Error] Could not load image at: {templatePath}");
                return null;
            }

            // 2. Format Normalization (The Fix)
            // We force both images to be standard 3-channel Color (BGR) so they match perfectly.
            using var screen3C = new Mat();
            using var template3C = new Mat();

            // If screen has 4 channels (transparent), convert to 3. If 3, just copy.
            if (screen.Channels() == 4) Cv2.CvtColor(screen, screen3C, ColorConversionCodes.BGRA2BGR);
            else screen.CopyTo(screen3C);

            // Same for template
            if (rawTemplate.Channels() == 4) Cv2.CvtColor(rawTemplate, template3C, ColorConversionCodes.BGRA2BGR);
            else rawTemplate.CopyTo(template3C);

            // 3. Perform the match
            using var result = new Mat();
            Cv2.MatchTemplate(screen3C, template3C, result, TemplateMatchModes.CCoeffNormed);

            Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
                int centerX = maxLoc.X + (template3C.Width / 2);
                int centerY = maxLoc.Y + (template3C.Height / 2);
                
                Console.WriteLine($"[Vision] Found {System.IO.Path.GetFileName(templatePath)} at ({centerX}, {centerY}) | Confidence: {maxVal:P0}");
                return new System.Drawing.Point(centerX, centerY);
            }

            return null;
        }
    }
}