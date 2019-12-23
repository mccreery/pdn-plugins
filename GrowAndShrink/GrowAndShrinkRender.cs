using System;
using System.Drawing;
using PaintDotNet;

namespace AssortedPlugins.GrowAndShrink
{
    public partial class GrowAndShrinkEffect
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
        }
    }
}
