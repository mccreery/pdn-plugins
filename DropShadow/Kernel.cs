using System;
using System.Drawing;
using PaintDotNet;

namespace AssortedPlugins.DropShadow
{
    public class Kernel
    {
        private readonly Size size;
        private readonly Size anchor;
        private readonly float[][] kernelAlpha;

        public Kernel(Bitmap image)
        {
            size = image.Size;
            anchor = new Size(image.Width / 2, image.Height / 2);

            kernelAlpha = new float[image.Height][];
            for(int y = 0; y < image.Height; y++)
            {
                float[] row = new float[image.Width];
                for(int x = 0; x < image.Width; x++)
                {
                    row[x] = image.GetPixel(x, y).A / 255.0f;
                }
                kernelAlpha[y] = row;
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
                float[] row = kernelAlpha[y - location.Y];

                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    byte alpha = surface[x, y].A;

                    alpha = (byte)Math.Round(alpha * row[x - location.X]);
                    maxAlpha = Math.Max(maxAlpha, alpha);

                    // Short circuit case - we can't get any more opaque
                    if (maxAlpha == 255)
                    {
                        return maxAlpha;
                    }
                }
            }
            return maxAlpha;
        }
    }
}
