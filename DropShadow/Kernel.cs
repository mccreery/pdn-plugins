using System;
using System.Drawing;
using PaintDotNet;

namespace AssortedPlugins.DropShadow
{
    public class Kernel
    {
        private readonly Size size;
        private readonly Size anchor;
        private readonly double[,] kernelAlpha;

        public Kernel(Bitmap image)
        {
            size = image.Size;
            anchor = new Size(image.Width / 2, image.Height / 2);

            kernelAlpha = new double[image.Height, image.Width];
            for(int y = 0; y < image.Height; y++)
            {
                for(int x = 0; x < image.Width; x++)
                {
                    kernelAlpha[y, x] = image.GetPixel(x, y).A / 255.0;
                }
            }
        }

        public byte WeightedMaxAlpha(Surface surface, Point center)
        {
            Point location = center - anchor;
            Rectangle bounds = new Rectangle(location, size);
            bounds.Intersect(surface.Bounds);

            byte maxAlpha = 0;
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    byte alpha = surface[x, y].A;

                    alpha = (byte)Math.Round(alpha * kernelAlpha[y - location.Y, x - location.X]);
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
}
