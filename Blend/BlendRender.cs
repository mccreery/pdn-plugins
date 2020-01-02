using PaintDotNet;
using System.Drawing;

namespace AssortedPlugins.Blend
{
    public partial class BlendEffect
    {
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            int endIndex = startIndex + length;

            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            BlendFunc blendFunc = this.blendMode.GetBlendFunc();
            ColorBgra colorOpaque = color.NewAlpha(255);

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra srcColor = src[x, y];
                    ColorBgra dstColor = srcColor;

                    if (blendColor)
                    {
                        byte tempAlpha = dstColor.A;
                        dstColor.A = 255;
                        dstColor = blendMode.Apply(dstColor, colorOpaque);
                        dstColor.A = tempAlpha;
                    }
                    if (blendAlpha)
                    {
                        dstColor.A = blendFunc(dstColor.A, color.A);
                    }
                    if (interpolateColor)
                    {
                        byte a = dstColor.A;
                        dstColor = ColorBgra.Lerp(srcColor, dstColor, ByteUtil.ToScalingFloat(color.A));
                        dstColor.A = a;
                    }
                    if (underlay)
                    {
                        dstColor = UserBlendOps.NormalBlendOp.ApplyStatic(color, dstColor);
                    }

                    dst[x, y] = dstColor;
                }
            }
        }
    }
}
