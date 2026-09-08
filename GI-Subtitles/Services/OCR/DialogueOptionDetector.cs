using System;
using OpenCvSharp;

namespace GI_Subtitles.Services.OCR
{
    /// <summary>
    /// Locates Genshin dialogue choices using a generated ellipsis template.
    /// The template is drawn at runtime so no third-party image asset is bundled.
    /// </summary>
    internal static class DialogueOptionDetector
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        public static bool TryFindTextRegion(
            Mat screen,
            out Rect textRegion,
            out double confidence,
            double threshold = 0.74)
        {
            textRegion = default;
            confidence = 0;
            if (screen == null || screen.Empty() || screen.Width < 640 || screen.Height < 360)
            {
                return false;
            }

            // Template matching has no benefit from operating on a native 6K/8K frame.
            // Keep the analysis frame at the detector's reference resolution and map
            // the resulting rectangle back to the caller's coordinate space.
            double analysisScale = Math.Min(
                1.0,
                Math.Min(
                    ReferenceWidth / (double)screen.Width,
                    ReferenceHeight / (double)screen.Height));
            using var analysis = new Mat();
            if (analysisScale < 1.0)
            {
                Cv2.Resize(
                    screen,
                    analysis,
                    new Size(),
                    analysisScale,
                    analysisScale,
                    InterpolationFlags.Area);
            }
            else
            {
                screen.CopyTo(analysis);
            }

            double scale = Math.Min(
                analysis.Width / (double)ReferenceWidth,
                analysis.Height / (double)ReferenceHeight);
            scale = Math.Max(0.5, Math.Min(3.0, scale));

            var searchRegion = new Rect(
                analysis.Width / 2,
                analysis.Height / 12,
                analysis.Width - analysis.Width / 2 - analysis.Width / 6,
                analysis.Height - analysis.Height / 12 - 10);

            using var gray = new Mat();
            if (analysis.Channels() == 1)
            {
                analysis.CopyTo(gray);
            }
            else
            {
                Cv2.CvtColor(
                    analysis,
                    gray,
                    analysis.Channels() == 4
                        ? ColorConversionCodes.BGRA2GRAY
                        : ColorConversionCodes.BGR2GRAY);
            }

            using var template = CreateTemplate(scale);
            if (searchRegion.Width < template.Width || searchRegion.Height < template.Height)
            {
                return false;
            }

            using var search = new Mat(gray, searchRegion);
            using var matches = new Mat();
            Cv2.MatchTemplate(search, template, matches, TemplateMatchModes.CCoeffNormed);

            Point lowestMatch = default;
            bool found = false;
            for (int i = 0; i < 8; i++)
            {
                Cv2.MinMaxLoc(matches, out _, out double maxValue, out _, out Point maxLocation);
                confidence = Math.Max(confidence, maxValue);
                if (maxValue < threshold)
                {
                    break;
                }

                if (!found || maxLocation.Y > lowestMatch.Y)
                {
                    lowestMatch = maxLocation;
                    found = true;
                }

                int suppressLeft = Math.Max(0, maxLocation.X - template.Width);
                int suppressTop = Math.Max(0, maxLocation.Y - template.Height);
                int suppressRight = Math.Min(matches.Width, maxLocation.X + template.Width * 2);
                int suppressBottom = Math.Min(matches.Height, maxLocation.Y + template.Height * 2);
                Cv2.Rectangle(
                    matches,
                    Rect.FromLTRB(suppressLeft, suppressTop, suppressRight, suppressBottom),
                    Scalar.All(-1),
                    -1);
            }

            if (!found)
            {
                return false;
            }

            int textLeft = searchRegion.X + lowestMatch.X + template.Width + (int)Math.Round(8 * scale);
            int textTop = analysis.Height / 12;
            int textRight = Math.Min(analysis.Width, textLeft + (int)Math.Round(535 * scale));
            int textBottom = Math.Min(
                analysis.Height,
                searchRegion.Y + lowestMatch.Y + template.Height + (int)Math.Round(30 * scale));

            if (textRight <= textLeft || textBottom <= textTop)
            {
                return false;
            }

            Rect analysisRegion = Rect.FromLTRB(textLeft, textTop, textRight, textBottom);
            textRegion = ScaleWithin(analysisRegion, 1.0 / analysisScale, screen.Size());
            return true;
        }

        private static Rect ScaleWithin(Rect source, double scale, Size bounds)
        {
            return Rect.FromLTRB(
                Math.Max(0, (int)Math.Floor(source.Left * scale)),
                Math.Max(0, (int)Math.Floor(source.Top * scale)),
                Math.Min(bounds.Width, (int)Math.Ceiling(source.Right * scale)),
                Math.Min(bounds.Height, (int)Math.Ceiling(source.Bottom * scale)));
        }

        private static Mat CreateTemplate(double scale)
        {
            const int baseWidth = 28;
            const int baseHeight = 10;
            using var source = new Mat(baseHeight, baseWidth, MatType.CV_8UC1, Scalar.All(255));
            foreach (int x in new[] { 5, 14, 23 })
            {
                Cv2.Circle(source, new Point(x, 5), 2, Scalar.All(105), -1, LineTypes.AntiAlias);
            }

            int width = Math.Max(8, (int)Math.Round(baseWidth * scale));
            int height = Math.Max(4, (int)Math.Round(baseHeight * scale));
            var resized = new Mat();
            Cv2.Resize(source, resized, new Size(width, height), 0, 0, InterpolationFlags.Cubic);
            return resized;
        }
    }
}
