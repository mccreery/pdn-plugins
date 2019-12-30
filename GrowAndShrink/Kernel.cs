using PaintDotNet;
using System;
using System.Drawing;

namespace AssortedPlugins.GrowAndShrink
{
    public class Kernel
    {
        public Point Anchor { get; }
        public Size Size { get; }

        public Rectangle Bounds => new Rectangle(Anchor.Negate(), Size);

        // Stores 0 for transparent pixels and 255 for opaque ones
        private readonly byte[][] mask;

        public Kernel(Bitmap image)
        {
            Size = image.Size;
            Anchor = new Point(image.Width / 2, image.Height / 2);

            mask = new byte[image.Height][];
            for (int y = 0; y < image.Height; y++)
            {
                byte[] row = mask[y] = new byte[image.Width];
                for (int x = 0; x < image.Width; x++)
                {
                    // Trick to replace all values < 128 to 0, and >= 128 to 255
                    row[x] = (byte)((sbyte)image.GetPixel(x, y).A >> 7);
                }
            }
        }

        public unsafe byte WeightedExtremeAlpha(Surface surface, int x, int y, bool min)
        {
            x -= Anchor.X;
            y -= Anchor.Y;

            // Precalculate bounds inside the surface
            int minY = Math.Max(y, 0);
            int maxY = Math.Min(y + Size.Height, surface.Height);

            int minX = Math.Max(x, 0);
            int maxX = Math.Min(x + Size.Width, surface.Width);

            byte maxAlpha = 0;
            for (int i = minY; i < maxY; i++)
            {
                ColorBgra* pixel = surface.GetPointAddressUnchecked(minX, i);
                byte[] row = mask[i - y];

                for (int j = minX; j < maxX; j++, pixel++)
                {
                    byte alpha = pixel->A;

                    // Treat transparent pixels as opaque
                    if (min)
                    {
                        alpha = (byte)(255 - alpha);
                    }

                    // Exclude pixels not in kernel
                    alpha &= row[j - x];

                    maxAlpha = Math.Max(maxAlpha, alpha);

                    // Short circuit case - we can't get any more opaque
                    if (maxAlpha == 255)
                    {
                        // hehe
                        goto Short;
                    }
                }
            }
        Short:
            // Treat transparent pixels as transparent again
            // Equivalently can be done inside the loop and using Math.Min
            if (min)
            {
                maxAlpha = (byte)(255 - maxAlpha);
            }
            return maxAlpha;
        }
    }
}
