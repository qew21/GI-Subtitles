using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    /// <summary>
    /// The tag colors of the result column's colored layer (ADR 0012): OCR
    /// blue and source green mirror the recognition-preview badges in
    /// Video.xaml, translation purple completes the trio. Content text never
    /// gets a color here — the null returned for <see cref="ActivityLogResultTag.None"/>
    /// means "inherit the default foreground", which is what keeps the two
    /// layers identical except for the tag color.
    /// </summary>
    public static class ActivityLogResultTagColors
    {
        private static readonly Dictionary<ActivityLogResultTag, SolidColorBrush> Brushes =
            new Dictionary<ActivityLogResultTag, SolidColorBrush>
            {
                { ActivityLogResultTag.Ocr, Frozen(ParseHex("#315EA8")) },
                { ActivityLogResultTag.Original, Frozen(ParseHex("#287A4B")) },
                { ActivityLogResultTag.Translation, Frozen(ParseHex("#8250DF")) }
            };

        /// <summary>A frozen, shareable brush of the tag color, or null to
        /// inherit the default foreground (content text, untagged lines).
        /// Frozen brushes are safe to hand to any run on any thread.</summary>
        public static Brush BrushFor(ActivityLogResultTag tag)
        {
            SolidColorBrush brush;
            return Brushes.TryGetValue(tag, out brush) ? brush : null;
        }

        private static SolidColorBrush ParseHex(string hex)
        {
            byte r = byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private static SolidColorBrush Frozen(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
