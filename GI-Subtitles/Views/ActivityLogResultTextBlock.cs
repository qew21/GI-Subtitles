using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    /// <summary>
    /// The colored layer of one result cell (ADR 0012): a TextBlock stacked
    /// under the cell's selection TextBox that renders the same projection
    /// with the result tags colored and the content left on the default
    /// foreground. The layers may differ in color only — this control must
    /// never set weight, family, or size on a run, or the selection
    /// rectangles stop matching the visible glyphs. It rebuilds whenever the
    /// <see cref="Lines"/> binding retargets, which is what virtualization
    /// recycling exercises: a recycled container gets a new DataContext, the
    /// binding re-fires, and the inlines are rebuilt from the new row.
    /// </summary>
    public sealed class ActivityLogResultTextBlock : TextBlock
    {
        public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
            "Lines",
            typeof(IReadOnlyList<ActivityLogResultLine>),
            typeof(ActivityLogResultTextBlock),
            new PropertyMetadata(OnLinesChanged));

        public IReadOnlyList<ActivityLogResultLine> Lines
        {
            get { return (IReadOnlyList<ActivityLogResultLine>)GetValue(LinesProperty); }
            set { SetValue(LinesProperty, value); }
        }

        private static void OnLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ActivityLogResultTextBlock)d).RebuildInlines();
        }

        private void RebuildInlines()
        {
            Inlines.Clear();

            IReadOnlyList<ActivityLogResultLine> lines = Lines;
            if (lines == null)
            {
                return;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                ActivityLogResultLine line = lines[i];
                if (i > 0)
                {
                    Inlines.Add(new LineBreak());
                }

                Brush tagBrush = ActivityLogResultTagColors.BrushFor(line.Tag);
                if (tagBrush != null && !string.IsNullOrEmpty(line.TagText))
                {
                    Inlines.Add(new Run(line.TagText) { Foreground = tagBrush });
                }

                Inlines.Add(new Run(line.ContentText));
            }
        }
    }
}
