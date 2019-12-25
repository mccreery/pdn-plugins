using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PaintDotNet;

namespace AssortedPlugins.GrowAndShrink
{
    public partial class GrowAndShrinkEffect
    {
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Kernel kernel = GetKernel();
            int endIndex = startIndex + length;

            for(int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i], kernel);
            }
        }

        private void Render(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            if(radius == 0)
            {
                dst.CopySurface(src, rect.Location, rect);
                return;
            }

            for(int y = rect.Top; y < rect.Bottom; y++)
            {
                if(IsCancelRequested) { return; }
                for(int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra blendedColor = ColorBgra.Blend(color, src[x, y], src[x, y].A);
                    byte alpha = kernel.WeightedMaxAlpha(src, x, y);

                    dst[x, y] = blendedColor.NewAlpha(alpha);
                }
            }
        }

        private Kernel GetKernel()
        {
            int size = radius*2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = smoothingMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillEllipse(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height);
            return new Kernel(bitmap);
        }
    }
}
