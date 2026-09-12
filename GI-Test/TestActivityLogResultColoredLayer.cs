using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GI_Subtitles.Core.Overlay;
using GI_Subtitles.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    /// <summary>
    /// The colored layer of the result column (ADR 0012): the tag palette —
    /// OCR blue, source green (the recognition-preview badge pair in
    /// Video.xaml), translation purple — and the TextBlock that renders the
    /// projection with the tags colored. The layer may differ from the
    /// selection TextBox in color only, so these tests pin both the palette
    /// and the no-weight-family-or-size invariant that keeps selection
    /// rectangles aligned with the visible glyphs.
    /// </summary>
    [TestClass]
    public class TestActivityLogResultColoredLayer
    {
        [TestMethod]
        public void BrushFor_ReturnsTheAdrPaletteAsFrozenBrushes()
        {
            AssertPalette(ActivityLogResultTag.Ocr, "#315EA8");
            AssertPalette(ActivityLogResultTag.Original, "#287A4B");
            AssertPalette(ActivityLogResultTag.Translation, "#8250DF");
            Assert.IsNull(ActivityLogResultTagColors.BrushFor(ActivityLogResultTag.None));
        }

        [TestMethod]
        public void ColoredLayer_ShowsExactlyTheProjectionText()
        {
            RunOnSta(() =>
            {
                ActivityLogResultProjection projection = ActivityLogResultComposerHarness.Compose(
                    ActivityLogResultComposerHarness.Row(ocrText: "hello", original: "你好", translation: "hello world"));

                var control = new ActivityLogResultTextBlock { Lines = projection.Lines };

                Assert.AreEqual(projection.PlainText, PlainTextOf(control));
            });
        }

        [TestMethod]
        public void ColoredLayer_ColorsTagRunsAndLeavesContentAndTypographyAlone()
        {
            RunOnSta(() =>
            {
                ActivityLogResultProjection projection = ActivityLogResultComposerHarness.Compose(
                    ActivityLogResultComposerHarness.Row(ocrText: "hello", original: "你好", translation: "hello world"));

                var control = new ActivityLogResultTextBlock { Lines = projection.Lines };

                var tagRuns = new List<Run>();
                var contentRuns = new List<Run>();
                foreach (Inline inline in control.Inlines)
                {
                    var run = inline as Run;
                    if (run == null)
                    {
                        continue;
                    }

                    if (HasLocalForeground(run))
                    {
                        tagRuns.Add(run);
                    }
                    else
                    {
                        contentRuns.Add(run);
                    }
                }

                Assert.AreEqual(3, tagRuns.Count);
                Assert.AreEqual(3, contentRuns.Count);
                Assert.AreSame(
                    ActivityLogResultTagColors.BrushFor(ActivityLogResultTag.Ocr),
                    tagRuns[0].ReadLocalValue(TextElement.ForegroundProperty));
                Assert.AreSame(
                    ActivityLogResultTagColors.BrushFor(ActivityLogResultTag.Original),
                    tagRuns[1].ReadLocalValue(TextElement.ForegroundProperty));
                Assert.AreSame(
                    ActivityLogResultTagColors.BrushFor(ActivityLogResultTag.Translation),
                    tagRuns[2].ReadLocalValue(TextElement.ForegroundProperty));
                Assert.AreEqual("[OCR]", tagRuns[0].Text);
                Assert.AreEqual("[原文]", tagRuns[1].Text);
                Assert.AreEqual("[译文]", tagRuns[2].Text);
                Assert.AreEqual("「hello」", contentRuns[0].Text);
                Assert.AreEqual("「你好」", contentRuns[1].Text);
                Assert.AreEqual("「hello world」", contentRuns[2].Text);

                // Color is the only thing the layers may differ in: bold,
                // italic, family, or size on any inline would shift glyph
                // advances under the transparent selection glyphs.
                foreach (Inline inline in control.Inlines)
                {
                    AssertTypographyUntouched(inline);
                }
            });
        }

        [TestMethod]
        public void ColoredLayer_RendersUntaggedLinesAsOneDefaultRun()
        {
            RunOnSta(() =>
            {
                ActivityLogResultProjection projection = ActivityLogResultComposerHarness.Compose(
                    ActivityLogResultComposerHarness.Row(detectionMiss: true));

                var control = new ActivityLogResultTextBlock { Lines = projection.Lines };

                Assert.AreEqual(projection.PlainText, PlainTextOf(control));
                foreach (Inline inline in control.Inlines)
                {
                    var run = inline as Run;
                    Assert.IsNotNull(run);
                    Assert.IsFalse(HasLocalForeground(run));
                    AssertTypographyUntouched(inline);
                }
            });
        }

        [TestMethod]
        public void ColoredLayer_RebuildsWhenTheLinesBindingRetargets()
        {
            RunOnSta(() =>
            {
                var first = new StubRow { ResultLines = ActivityLogResultComposerHarness.Compose(
                    ActivityLogResultComposerHarness.Row(ocrText: "hello", original: "你好", translation: "hello world")).Lines };
                var second = new StubRow { ResultLines = ActivityLogResultComposerHarness.Compose(
                    ActivityLogResultComposerHarness.Row(detectionMiss: true)).Lines };

                var control = new ActivityLogResultTextBlock();
                BindingOperations.SetBinding(
                    control,
                    ActivityLogResultTextBlock.LinesProperty,
                    new Binding("ResultLines"));
                control.DataContext = first;
                PumpDispatcher(control);
                Assert.AreEqual(6, CountRuns(control));
                Assert.AreEqual(3, CountColoredRuns(control));

                // Container recycling: the DataContext swaps, the binding
                // re-fires, and the inlines must come from the new row.
                control.DataContext = second;
                PumpDispatcher(control);
                Assert.AreEqual(1, CountRuns(control));
                Assert.AreEqual(0, CountColoredRuns(control));
            });
        }

        private static void AssertPalette(ActivityLogResultTag tag, string expectedHex)
        {
            Brush brush = ActivityLogResultTagColors.BrushFor(tag);
            var solid = brush as SolidColorBrush;
            Assert.IsNotNull(solid, tag.ToString());
            Assert.IsTrue(solid.IsFrozen, tag.ToString());
            Assert.AreEqual(
                (Color)ColorConverter.ConvertFromString(expectedHex),
                solid.Color,
                tag.ToString());
        }

        private static string PlainTextOf(ActivityLogResultTextBlock control)
        {
            var builder = new StringBuilder();
            foreach (Inline inline in control.Inlines)
            {
                var run = inline as Run;
                if (run != null)
                {
                    builder.Append(run.Text);
                }
                else if (inline is LineBreak)
                {
                    builder.Append(Environment.NewLine);
                }
            }

            return builder.ToString();
        }

        private static int CountRuns(ActivityLogResultTextBlock control)
        {
            int count = 0;
            foreach (Inline inline in control.Inlines)
            {
                if (inline is Run)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountColoredRuns(ActivityLogResultTextBlock control)
        {
            int count = 0;
            foreach (Inline inline in control.Inlines)
            {
                var run = inline as Run;
                if (run != null && HasLocalForeground(run))
                {
                    count++;
                }
            }

            return count;
        }

        // Run.Foreground is an inheriting property whose effective value is
        // never null; a run carries a tag color only when it set the
        // Foreground locally.
        private static bool HasLocalForeground(Run run)
        {
            return run.ReadLocalValue(TextElement.ForegroundProperty) != DependencyProperty.UnsetValue;
        }

        private static void AssertTypographyUntouched(Inline inline)
        {
            Assert.AreEqual(
                DependencyProperty.UnsetValue,
                inline.ReadLocalValue(TextElement.FontWeightProperty),
                "FontWeight must stay unset (color-only layer)");
            Assert.AreEqual(
                DependencyProperty.UnsetValue,
                inline.ReadLocalValue(TextElement.FontStyleProperty),
                "FontStyle must stay unset (color-only layer)");
            Assert.AreEqual(
                DependencyProperty.UnsetValue,
                inline.ReadLocalValue(TextElement.FontFamilyProperty),
                "FontFamily must stay unset (color-only layer)");
            Assert.AreEqual(
                DependencyProperty.UnsetValue,
                inline.ReadLocalValue(TextElement.FontSizeProperty),
                "FontSize must stay unset (color-only layer)");
        }

        // A binding on a disconnected element defers its value transfer to
        // the dispatcher; push a frame so it runs, as a layout pass would.
        private static void PumpDispatcher(ActivityLogResultTextBlock control)
        {
            var frame = new DispatcherFrame();
            control.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(delegate { frame.Continue = false; }));
            Dispatcher.PushFrame(frame);
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(delegate()
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    failure = e;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                throw failure;
            }
        }

        private sealed class StubRow
        {
            public IReadOnlyList<ActivityLogResultLine> ResultLines { get; set; }
        }
    }
}
