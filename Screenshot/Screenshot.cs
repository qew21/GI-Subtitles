using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;
using Size = System.Drawing.Size;

namespace Screenshot
{
    public class Screenshot
    {
        public static BitmapSource CaptureAllScreens(double scale = 1.0)
        {
            return CaptureRegion(new Rect(SystemParameters.VirtualScreenLeft * scale,
                                          SystemParameters.VirtualScreenTop * scale,
                                          SystemParameters.VirtualScreenWidth * scale,
                                          SystemParameters.VirtualScreenHeight * scale));
        }


        public static Rect GetRegion(double scale)
        {
            var options = new ScreenshotOptions();

            var bitmap = CaptureAllScreens(scale);

            var left = SystemParameters.VirtualScreenLeft * scale;
            var top = SystemParameters.VirtualScreenTop * scale;
            var right = left + SystemParameters.VirtualScreenWidth * scale;
            var bottom = right + SystemParameters.VirtualScreenHeight * scale;

            var window = new RegionSelectionWindow
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                ShowInTaskbar = false,
                BorderThickness = new Thickness(0),
                BackgroundImage =
                             {
                                 Source = bitmap,
                                 Opacity = options.BackgroundOpacity
                             },
                InnerBorder = { BorderBrush = options.SelectionRectangleBorderBrush },
                Left = left,
                Top = top,
                Width = right - left,
                Height = bottom - top
            };

            window.ShowDialog();

            return window.SelectedRegion.Value;
        }

        public static BitmapSource CaptureRegion(Rect rect)
        {
            using (var bitmap = new Bitmap((int)rect.Width, (int)rect.Height, PixelFormat.Format32bppArgb))
            {
                var graphics = Graphics.FromImage(bitmap);

                graphics.CopyFromScreen((int)rect.X, (int)rect.Y, 0, 0, new Size((int)rect.Size.Width, (int)rect.Size.Height),
                                        CopyPixelOperation.SourceCopy);

                return bitmap.ToBitmapSource();
            }
        }

        private static BitmapSource GetBitmapRegion(BitmapSource bitmap, Rect rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            return new CroppedBitmap(bitmap, new Int32Rect
            {
                X = (int)rect.X,
                Y = (int)rect.Y,
                Width = (int)rect.Width,
                Height = (int)rect.Height
            });
        }
    }
}