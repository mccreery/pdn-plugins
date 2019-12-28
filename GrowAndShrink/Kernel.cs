using PaintDotNet;
using System;
using System.Drawing;

namespace AssortedPlugins.GrowAndShrink
{
    public class Kernel
    {
        public Point Anchor { get; }
        public Size Size { get; }

        private readonly double[,] kernelAlpha;

        public Kernel(Bitmap image)
        {
            Size = image.Size;
            Anchor = new Point(image.Width / 2, image.Height / 2);

            kernelAlpha = new double[image.Height, image.Width];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    kernelAlpha[y, x] = image.GetPixel(x, y).A / 255.0;
                }
            }
        }

        public byte WeightedExtremeAlpha(Surface surface, int x, int y, bool min)
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
                for (int j = minX; j < maxX; j++)
                {
                    byte alpha = surface[j, i].A;

                    // Treat transparent pixels as opaque
                    if (min)
                    {
                        alpha = (byte)(255 - alpha);
                    }
                    alpha = (byte)Math.Round(alpha * kernelAlpha[i - y, j - x]);
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
