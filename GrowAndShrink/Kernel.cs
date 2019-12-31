using PaintDotNet;
using System;
using System.Drawing;

namespace AssortedPlugins.GrowAndShrink
{
    public class Kernel
    {
        /** <value>The kernel's bounding box centered around the origin.</value> */
        public Rectangle Bounds { get; }

        // Bitmask for each pixel (row-major access [y][x])
        private readonly byte[][] mask;

        public Kernel(Bitmap image)
        {
            // Centered around the origin
            Bounds = new Rectangle(
                -image.Width / 2,
                -image.Height / 2,
                image.Width,
                image.Height);

            mask = new byte[Bounds.Height][];
            for (int y = 0; y < mask.Length; y++)
            {
                byte[] maskRow = new byte[Bounds.Width];
                for (int x = 0; x < maskRow.Length; x++)
                {
                    // Mostly opaque pixels are included in the mask
                    maskRow[x] = (byte)(image.GetPixel(x, y).A >= 128 ? 255 : 0);
                }
                mask[y] = maskRow;
            }
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

            // The same bounds after clipping to prevent access to memory outside the surface
            Rectangle clampedRect = rect;
            clampedRect.Intersect(surface.Bounds);

            byte maxAlpha = 0;
            for (int y = clampedRect.Top; y < clampedRect.Bottom; y++)
            {
                // Unsafe surface access is more efficient
                ColorBgra* rowPtr = surface.GetRowAddressUnchecked(y);
                byte[] maskRow = mask[y - rect.Top];

                for (int x = clampedRect.Left; x < clampedRect.Right; x++)
                {
                    byte alpha = rowPtr[x].A;
                    // Invert alpha value (first time) to find minimum
                    if (min)
                    {
                        alpha = (byte)~alpha;
                    }
                    // Masking has been more efficient while profiling
                    alpha &= maskRow[x - rect.Left];

                    maxAlpha = Math.Max(maxAlpha, alpha);
                    // Shortcut since 255 is the maximum byte value
                    if (maxAlpha == 255)
                    {
                        goto BreakOuter;
                    }
                }
            }
        BreakOuter:
            // Invert alpha value (second time) to find minimum
            if (min)
            {
                maxAlpha = (byte)~maxAlpha;
            }
            return maxAlpha;
        }
    }
}
