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

            void UpdatePixel(int x, int y)
            {
                ColorBgra blendedColor = ColorBgra.Blend(color, src[x, y], src[x, y].A);
                byte alpha = kernel.WeightedExtremeAlpha(src, x, y, radius < 0);

                dst[x, y] = blendedColor.NewAlpha(alpha);
            }

            Rectangle influenceMargin = new Rectangle(kernel.Anchor.Negate(), kernel.Size);
            Rectangle influenceBounds = rect.Inflate(influenceMargin);
            influenceBounds.Intersect(src.Bounds);

            BitMask mask = new BitMask(rect);
            Point point = new Point();

            for (point.Y = influenceBounds.Top; point.Y < influenceBounds.Bottom; point.Y++)
            {
                for(point.X = influenceBounds.Left; point.X < influenceBounds.Right; point.X++)
                {
                    byte a = src[point].A;
                    if(a != 0 && a != 255)
                    {
                        mask.MarkRect(new Rectangle(point - (Size)kernel.Anchor, kernel.Size));
                    }
                }
            }

            foreach ((Point p, bool marked) in mask)
            {
                dst[p] = marked ? ColorBgra.Black : ColorBgra.TransparentBlack;
            }
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
