using System;
using System.Drawing;
using PaintDotNet;

namespace AssortedPlugins.Blend
{
    public partial class BlendEffect
    {
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            int endIndex = startIndex + length;

            for(int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            for(int y = rect.Top; y < rect.Bottom; y++)
            {
                for(int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = blendMode.Apply(src[x, y], color);
                }
            }
        }
    }
}
