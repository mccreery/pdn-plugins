using System.Drawing;

namespace AssortedPlugins
{
    public static class Extensions
    {
        public static Rectangle Inflate(this Rectangle rect, Rectangle margins)
        {
            return Inflate(rect, -margins.Left, -margins.Top, margins.Right, margins.Bottom);
        }

        public static Rectangle Inflate(this Rectangle rect, int left, int top, int right, int bottom)
        {
            return Rectangle.FromLTRB(rect.Left - left, rect.Top - top, rect.Right + right, rect.Bottom + bottom);
        }

        public static Point Negate(this Point point)
        {
            return new Point(-point.X, -point.Y);
        }
    }
}
