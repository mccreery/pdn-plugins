using System;
using System.Drawing;
using PaintDotNet;

namespace AssortedPlugins.DropShadow
{
    public class Kernel
    {
        private readonly Size size;
        private readonly Point anchor;
        private readonly double[,] kernelAlpha;

        public Kernel(Bitmap image)
        {
            size = image.Size;
            anchor = new Point(image.Width / 2, image.Height / 2);

            kernelAlpha = new double[image.Height, image.Width];
            for(int y = 0; y < image.Height; y++)
            {
                for(int x = 0; x < image.Width; x++)
                {
                    kernelAlpha[y, x] = image.GetPixel(x, y).A / 255.0;
                }
            }
        }

        public byte WeightedMaxAlpha(Surface surface, int x, int y)
        {
            x -= anchor.X;
            y -= anchor.Y;

            byte maxAlpha = 0;
            for(int yOffset = 0; yOffset < size.Height; yOffset++)
            {
                for(int xOffset = 0; xOffset < size.Width; xOffset++)
                {
                    byte alpha = surface.GetPointZeroPad(x + xOffset, y + yOffset).A;

                    alpha = (byte)Math.Round(alpha * kernelAlpha[yOffset, xOffset]);
                    maxAlpha = Math.Max(maxAlpha, alpha);

                    // Short circuit case - we can't get any more opaque
                    if(maxAlpha == 255)
                    {
                        return maxAlpha;
                    }
                }
            }
            return maxAlpha;
        }
    }

    public static class SurfaceExtensions
    {
        public static ColorBgra GetPointZeroPad(this Surface surface, int x, int y)
        {
            return surface.Bounds.Contains(x, y) ? surface[x, y] : ColorBgra.TransparentBlack;
        }
    }
}
