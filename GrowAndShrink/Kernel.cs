using PaintDotNet;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AssortedPlugins.GrowAndShrink
{
    public class Kernel
    {
        /** <value>The kernel's bounding box centered around the origin.</value> */
        public Rectangle Bounds { get; }

        struct Range
        {
            public int lo;
            public int hi;
        }
        private Range[] ranges;

        public Kernel(Bitmap image)
        {
            // Centered around the origin
            Bounds = new Rectangle(
                -image.Width / 2,
                -image.Height / 2,
                image.Width,
                image.Height);

            ranges = new Range[Bounds.Height];
            for (int y = 0; y < ranges.Length; y++)
            {
                int lo, hi;
                for (lo = 0; lo < image.Width && image.GetPixel(lo, y).A < 128; lo++) ;
                for (hi = image.Width; --hi >= 0 && image.GetPixel(hi, y).A < 128;) ;
                ++hi;

                ranges[y].lo = lo;
                ranges[y].hi = hi;
            }
        }

        public static Kernel CreateCircle(int radius)
        {
            if (radius < 0)
            {
                throw new ArgumentException("Negative radius");
            }
            int size = radius * 2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillEllipse(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height);

            return new Kernel(bitmap);
        }

        /**
         * <summary>Finds the maximum or minimum alpha value in the neighborhood.</summary>
         * <param name="surface">The surface to search.</param>
         * <param name="point">The origin of the search, with the neighborhood centered around it.</param>
         * <param name="min">Set to <c>true</c> to find the minimum alpha instead of maximum.</param>
         * <returns>The maximum (default) or minimum alpha value in the neighborhood around <paramref name="point"/>.</returns>
         */
        public unsafe byte ExtremeAlpha(Surface surface, Point point, bool min = false)
        {
            // The bounds of the neighborhood relative to the surface
            Rectangle rect = Bounds;
            rect.Offset(point);

            // Avoid repeating min/max inside and outside the loop
            int top = Math.Max(surface.Bounds.Top, rect.Top);
            int bottom = Math.Min(surface.Bounds.Bottom, rect.Bottom);

            byte maxAlpha = 0;
            for (int y = top; y < bottom; y++)
            {
                // Unsafe surface access is more efficient
                ColorBgra* rowPtr = surface.GetRowAddressUnchecked(y);

                Range range = ranges[y - rect.Top];
                int left = Math.Max(surface.Bounds.Left, rect.Left + range.lo);
                int right = Math.Min(surface.Bounds.Right, rect.Left + range.hi);

                for (int x = left; x < right; x++)
                {
                    // Invert alpha (first time) to find minimum
                    byte alpha = min ? (byte)~rowPtr[x].A : rowPtr[x].A;

                    if (alpha > maxAlpha)
                    {
                        maxAlpha = alpha;
                        if (maxAlpha == 255)
                        {
                            goto BreakOuter;
                        }
                    }
                }
            }
        BreakOuter:
            // Invert alpha (second time) to find minimum
            return min ? (byte)~maxAlpha : maxAlpha;
        }
    }
}
