using System.Drawing;

namespace AssortedPlugins
{
    public static class RectangleExtensions
    {
        /// <summary>
        /// Adds corresponding components of two rectangles.
        /// </summary>
        /// <returns>A new rectangle that is the sum of the arguments.</returns>
        public static Rectangle Add(this Rectangle a, Rectangle b)
        {
            return new Rectangle(a.X + b.X, a.Y + b.Y, a.Width + b.Width, a.Height + b.Height);
        }
    }
}
