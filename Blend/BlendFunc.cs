using PaintDotNet;
using PaintDotNet.Rendering;
using System;
using static PaintDotNet.LayerBlendMode;

namespace AssortedPlugins.Blend
{
    public delegate byte BlendFunc(byte a, byte b);

    public static class BlendFuncUtil
    {
        public static BlendFunc GetBlendFunc(this BinaryPixelOp op)
        {
            return delegate (byte a, byte b)
            {
                ColorBgra colorA = ColorBgra.FromBgr(0, 0, a);
                ColorBgra colorB = ColorBgra.FromBgr(0, 0, b);

                ColorBgra result = op.Apply(colorA, colorB);
                return result.R;
            };
        }
    }
}
