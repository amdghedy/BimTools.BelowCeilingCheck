using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BimTools.BelowCeilingCheck.Infrastructure
{
    /// <summary>
    /// Draws the ribbon icon in code so the add-in has no external image resources.
    /// </summary>
    public static class IconFactory
    {
        public static BitmapSource CreateIcon(int size)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                double scale = size / 32.0;
                dc.PushTransform(new ScaleTransform(scale, scale));

                Brush background = new SolidColorBrush(Color.FromRgb(0x1F, 0x3A, 0x5F));
                Brush slab = new SolidColorBrush(Color.FromRgb(0xCF, 0xD8, 0xDC));
                Brush duct = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                Brush pipe = new SolidColorBrush(Color.FromRgb(0x29, 0xB6, 0xF6));
                Brush alert = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
                Pen hanger = new Pen(new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)), 1.5);
                Pen outline = new Pen(Brushes.White, 1.0);

                dc.DrawRoundedRectangle(background, null, new Rect(0, 0, 32, 32), 5, 5);
                dc.DrawRectangle(slab, null, new Rect(3, 5, 26, 4));
                dc.DrawLine(hanger, new Point(9, 9), new Point(9, 15));
                dc.DrawLine(hanger, new Point(23, 9), new Point(23, 15));
                dc.DrawRectangle(duct, outline, new Rect(5, 15, 22, 6));
                dc.DrawEllipse(pipe, outline, new Point(11, 26), 3, 3);
                dc.DrawEllipse(alert, outline, new Point(24, 26), 4, 4);

                dc.Pop();
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
    }
}
