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

                // Tries to find ANY image from a list. Returns the first one found.
        public static System.Drawing.Point? FindAny(Mat screen, string[] templates, double threshold = 0.8)
        {
            foreach (var template in templates)
            {
                var result = FindTemplate(screen, template, threshold);
                if (result != null) return result;
            }
            return null;
        }
        
        public static List<System.Drawing.Point> FindAllTemplates(Mat screen, string templatePath, double threshold = 0.8)
        {
            var matches = new List<System.Drawing.Point>();

            using var template = Cv2.ImRead(templatePath, ImreadModes.Color);
            if (template.Empty()) return matches;

            // 1. Normalize Colors
            using var screen3C = new Mat();
            using var template3C = new Mat();
            if (screen.Channels() == 4) Cv2.CvtColor(screen, screen3C, ColorConversionCodes.BGRA2BGR);
            else screen.CopyTo(screen3C);

            if (template.Channels() == 4) Cv2.CvtColor(template, template3C, ColorConversionCodes.BGRA2BGR);
            else template.CopyTo(template3C);

            using var result = new Mat();
            Cv2.MatchTemplate(screen3C, template3C, result, TemplateMatchModes.CCoeffNormed);

            while (true)
            {
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal >= threshold)
                {
                    int centerX = maxLoc.X + (template.Width / 2);
                    int centerY = maxLoc.Y + (template.Height / 2);
                    
                    // --- NEW: DUPLICATE CHECK ---
                    bool isDuplicate = false;
                    foreach (var existing in matches)
                    {
                        // Calculate distance (Pythagoras)
                        double distance = Math.Sqrt(Math.Pow(existing.X - centerX, 2) + Math.Pow(existing.Y - centerY, 2));
                        
                        // If the new match is within 20 pixels of an existing one, it's a ghost/duplicate.
                        if (distance < 20) 
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        matches.Add(new System.Drawing.Point(centerX, centerY));
                    }
                    // -----------------------------

                    // "Erase" this match from the result so we find the next distinct one
                    // We flood fill a slightly larger area to be safe
                    Cv2.FloodFill(result, maxLoc, new Scalar(0)); 
                }
                else
                {
                    break;
                }
            }

            // Sort by Y coordinate (Top first) so Index 0 is always Top, Index 1 is Middle.
            return matches.OrderBy(p => p.Y).ToList();
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