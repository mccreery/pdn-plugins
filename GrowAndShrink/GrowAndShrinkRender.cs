using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AssortedPlugins.GrowAndShrink
{
    public partial class GrowAndShrinkEffect
    {
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Kernel kernel = GetKernel();
            int endIndex = startIndex + length;

            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i], kernel);
            }
        }

        private void Render(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            if (radius == 0)
            {
                dst.CopySurface(src, rect.Location, rect);
                return;
            }

            BitMask mask = GetMask(src, rect, kernel);

            foreach ((Point point, bool marked) in mask)
            {
                if (marked)
                {
                    ColorBgra dstColor = src[point];
                    dstColor = ColorBgra.Blend(color, dstColor, dstColor.A);

                    byte alpha = kernel.WeightedExtremeAlpha(src, point.X, point.Y, radius < 0);

                    dst[point] = dstColor.NewAlpha(alpha);
                }
                else
                {
                    dst[point] = src[point];
                }
            }
        }

        private BitMask GetMask(Surface src, Rectangle rect, Kernel kernel)
        {
            BitMask mask = new BitMask(rect);
            Rectangle influence = rect.Inflate(kernel.Bounds);
            influence.Intersect(src.Bounds);

            Point point = new Point();
            for (point.Y = influence.Top; point.Y < influence.Bottom; point.Y++)
            {
                for (point.X = influence.Left; point.X < influence.Right; point.X++)
                {
                    byte a = src[point].A;
                    if (a != 0 && a != 255)
                    {
                        mask.MarkRect(new Rectangle(point - (Size)kernel.Anchor, kernel.Size));
                    }
                }
            }
            return mask;
        }

        private Kernel GetKernel()
        {
            int size = Math.Abs(radius) * 2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = smoothingMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillEllipse(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height);
            return new Kernel(bitmap);
        }
    }
}
