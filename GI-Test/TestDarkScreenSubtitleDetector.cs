using GI_Subtitles.Services.OCR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCvSharp;

namespace GI_Test
{
    [TestClass]
    public class DarkScreenSubtitleDetectorTests
    {
        [TestMethod]
        public void CenteredWhiteTextOnBlack_IsDetected()
        {
            using (var image = new Mat(540, 1200, MatType.CV_8UC3, Scalar.Black))
            {
                Cv2.PutText(
                    image,
                    "Previously, the witch told the little girl about the flower",
                    new Point(90, 245),
                    HersheyFonts.HersheySimplex,
                    0.95,
                    Scalar.White,
                    2,
                    LineTypes.AntiAlias);
                Cv2.PutText(
                    image,
                    "What happened after that, and was her friend rescued?",
                    new Point(130, 290),
                    HersheyFonts.HersheySimplex,
                    0.95,
                    Scalar.White,
                    2,
                    LineTypes.AntiAlias);

                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out Rect region,
                    out bool isDark,
                    out double darkRatio,
                    out _);

                Assert.IsTrue(isDark);
                Assert.IsTrue(found);
                Assert.IsTrue(darkRatio > 0.90);
                Assert.IsTrue(region.Width > image.Width * 0.50);
                Assert.IsTrue(region.Top < 230 && region.Bottom > 270);
            }
        }

        [TestMethod]
        public void PureBlackFrame_IsDarkButHasNoSubtitleCandidate()
        {
            using (var image = new Mat(540, 1200, MatType.CV_8UC3, Scalar.Black))
            {
                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out _,
                    out bool isDark,
                    out _,
                    out _);

                Assert.IsTrue(isDark);
                Assert.IsFalse(found);
            }
        }

        [TestMethod]
        public void YellowContinuePrompt_IsNotTreatedAsSubtitle()
        {
            using (var image = new Mat(540, 1200, MatType.CV_8UC3, Scalar.Black))
            {
                Cv2.PutText(
                    image,
                    "Click to continue",
                    new Point(430, 290),
                    HersheyFonts.HersheySimplex,
                    1.0,
                    new Scalar(0, 210, 255),
                    2,
                    LineTypes.AntiAlias);

                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out _,
                    out bool isDark,
                    out _,
                    out _);

                Assert.IsTrue(isDark);
                Assert.IsFalse(found);
            }
        }

        [TestMethod]
        public void MultiLineCenterText_IsPreferredOverBottomContinuePrompt()
        {
            using (var image = new Mat(540, 1200, MatType.CV_8UC3, Scalar.Black))
            {
                string[] lines =
                {
                    "There was another traveler who spoke in an ancient tongue",
                    "That person had been using us from the very beginning",
                    "It reminded you of the complicated feelings once buried",
                    "The young dragon was still small but would grow stronger",
                    "One day it would become a truly magnificent dragon"
                };

                for (int index = 0; index < lines.Length; index++)
                {
                    Cv2.PutText(
                        image,
                        lines[index],
                        new Point(80 + index * 8, 170 + index * 42),
                        HersheyFonts.HersheySimplex,
                        0.72,
                        Scalar.White,
                        2,
                        LineTypes.AntiAlias);
                }

                Cv2.PutText(
                    image,
                    "Click to continue",
                    new Point(480, 475),
                    HersheyFonts.HersheySimplex,
                    0.65,
                    Scalar.White,
                    2,
                    LineTypes.AntiAlias);

                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out Rect region,
                    out bool isDark,
                    out _,
                    out _);

                Assert.IsTrue(isDark);
                Assert.IsTrue(found);
                Assert.IsTrue(region.Top < 180, $"Expected the first text row, got {region}");
                Assert.IsTrue(region.Bottom > 350, $"Expected the last text row, got {region}");
                Assert.IsTrue(region.Bottom < 440, $"Continue prompt should be excluded, got {region}");
            }
        }

        [TestMethod]
        public void BrightScene_DoesNotEnterDarkScreenMode()
        {
            using (var image = new Mat(540, 1200, MatType.CV_8UC3, Scalar.All(180)))
            {
                Cv2.PutText(
                    image,
                    "Normal subtitle",
                    new Point(350, 280),
                    HersheyFonts.HersheySimplex,
                    1.0,
                    Scalar.White,
                    2,
                    LineTypes.AntiAlias);

                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out _,
                    out bool isDark,
                    out _,
                    out _);

                Assert.IsFalse(isDark);
                Assert.IsFalse(found);
            }
        }

        [TestMethod]
        public void UltraHighResolutionFrame_ReturnsCoordinatesInOriginalSpace()
        {
            using (var image = new Mat(4320, 7680, MatType.CV_8UC1, Scalar.Black))
            {
                Cv2.PutText(
                    image,
                    "A high resolution subtitle should be analyzed at a bounded size",
                    new Point(1040, 2240),
                    HersheyFonts.HersheySimplex,
                    6.4,
                    Scalar.White,
                    16,
                    LineTypes.AntiAlias);

                bool found = DarkScreenSubtitleDetector.TryFindSubtitleRegion(
                    image,
                    out Rect region,
                    out bool isDark,
                    out _,
                    out _);

                Assert.IsTrue(isDark);
                Assert.IsTrue(found);
                Assert.IsTrue(region.Width > 3600, $"Expected original-space width, got {region}");
                Assert.IsTrue(region.Top > 1800 && region.Bottom < 2600, $"Unexpected mapped region: {region}");
            }
        }
    }

    [TestClass]
    public class DialogueOptionDetectorTests
    {
        [TestMethod]
        public void UltraHighResolutionFrame_ReturnsCoordinatesInOriginalSpace()
        {
            using (var image = new Mat(4320, 7680, MatType.CV_8UC1, Scalar.White))
            {
                // The production detector creates the same 28x10 reference template.
                // Draw it at 4x so downsampling an 8K frame to 1080p preserves the pattern.
                const int markerX = 5600;
                const int markerY = 2440;
                Cv2.Rectangle(image, new Rect(markerX, markerY, 112, 40), Scalar.White, -1);
                foreach (int x in new[] { 20, 56, 92 })
                {
                    Cv2.Circle(
                        image,
                        new Point(markerX + x, markerY + 20),
                        8,
                        Scalar.All(105),
                        -1,
                        LineTypes.AntiAlias);
                }

                bool found = DialogueOptionDetector.TryFindTextRegion(
                    image,
                    out Rect region,
                    out double confidence,
                    0.70);

                Assert.IsTrue(found, $"Expected marker match, confidence={confidence:F3}");
                Assert.IsTrue(region.Left > markerX, $"Expected original-space coordinates, got {region}");
                Assert.IsTrue(region.Right > 7200, $"Expected a scaled text region, got {region}");
            }
        }
    }
}
