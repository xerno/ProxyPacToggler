using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Ui
{
    internal static class IconFactory
    {
        private const int Size = 32;
        private const int Inset = 3;
        private const int BadgeSize = 15;

        private static readonly Color On = Color.FromArgb(60, 200, 80);
        private static readonly Color Off = Color.FromArgb(210, 60, 60);
        private static readonly Color Warning = Color.FromArgb(245, 170, 20);

        // The badge goes on top so a problem never hides the PAC state.
        public static Icon Create(bool pacEnabled, bool hasProblem)
        {
            using (Bitmap bitmap = new Bitmap(Size, Size))
            {
                using (Graphics canvas = Graphics.FromImage(bitmap))
                {
                    canvas.SmoothingMode = SmoothingMode.AntiAlias;
                    canvas.Clear(Color.Transparent);
                    DrawDisc(canvas, pacEnabled ? On : Off);
                    if (hasProblem) DrawWarningBadge(canvas);
                }
                return OwnedCopyOf(bitmap);
            }
        }

        private static void DrawDisc(Graphics canvas, Color fill)
        {
            int diameter = Size - 2 * Inset;
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen outline = new Pen(Color.FromArgb(200, 0, 0, 0), 2f))
            using (Font font = new Font(FontFamily.GenericSansSerif, 14f, FontStyle.Bold,
                                        GraphicsUnit.Pixel))
            using (StringFormat centred = new StringFormat())
            {
                canvas.FillEllipse(brush, Inset, Inset, diameter, diameter);
                canvas.DrawEllipse(outline, Inset, Inset, diameter, diameter);

                centred.Alignment = StringAlignment.Center;
                centred.LineAlignment = StringAlignment.Center;
                canvas.DrawString("P", font, Brushes.White,
                                  new RectangleF(Inset, Inset + 1, diameter, diameter), centred);
            }
        }

        private static void DrawWarningBadge(Graphics canvas)
        {
            int left = Size - BadgeSize - 1;
            int top = Size - BadgeSize - 1;

            using (SolidBrush fill = new SolidBrush(Warning))
            using (Pen outline = new Pen(Color.FromArgb(230, 30, 20, 0), 1.5f))
            using (Font font = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold,
                                        GraphicsUnit.Pixel))
            using (StringFormat centred = new StringFormat())
            {
                canvas.FillEllipse(fill, left, top, BadgeSize, BadgeSize);
                canvas.DrawEllipse(outline, left, top, BadgeSize, BadgeSize);

                centred.Alignment = StringAlignment.Center;
                centred.LineAlignment = StringAlignment.Center;
                canvas.DrawString("!", font, Brushes.Black,
                                  new RectangleF(left, top, BadgeSize, BadgeSize), centred);
            }
        }

        private static Icon OwnedCopyOf(Bitmap bitmap)
        {
            IntPtr handle = bitmap.GetHicon();
            try
            {
                using (Icon unowned = Icon.FromHandle(handle))
                {
                    return (Icon)unowned.Clone();
                }
            }
            finally
            {
                // Icon.FromHandle does not take ownership of the handle.
                Native.DestroyIcon(handle);
            }
        }
    }
}
