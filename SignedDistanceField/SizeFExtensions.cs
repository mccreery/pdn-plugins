using System;
using System.Drawing;

namespace AssortedPlugins.SignedDistanceField
{
    static class SizeFExtensions
    {
        public static float MagnitudeSquared(this SizeF size)
        {
            return size.Width * size.Width + size.Height * size.Height;
        }

        public static int MagnitudeSquared(this Size size)
        {
            return size.Width * size.Width + size.Height * size.Height;
        }

        public static float Magnitude(this SizeF size)
        {
            return (float)Math.Sqrt(size.MagnitudeSquared());
        }

        public static float Magnitude(this Size size)
        {
            return (float)Math.Sqrt(size.MagnitudeSquared());
        }
    }
}
