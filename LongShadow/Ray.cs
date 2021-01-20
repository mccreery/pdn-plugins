using PaintDotNet;
using System;
using System.Drawing;

namespace AssortedPlugins.LongShadow
{
    public sealed class Ray
    {
        public PointF Origin { get; }
        public SizeF Direction { get; }

        public Ray(PointF origin, SizeF direction)
        {
            Origin = origin;
            Direction = direction;

            // Normalize direction
            float length = (float)Math.Sqrt(direction.Width * direction.Width + direction.Height * direction.Height);
            direction.Width /= length;
            direction.Height /= length;
        }

        public PointF this[float t] => new PointF(
            Origin.X + Direction.Width * t,
            Origin.Y + Direction.Height * t);

        public PointF? Trace(RenderArgs src, Predicate<ColorBgra> hit, bool backtrack = true)
        {
            float t;
            if (backtrack)
            {
                t = TraceEdge(src.Bounds, true);
            }
            else
            {
                t = 0;
            }

            for (; ; t += 1.0f)
            {
                PointF point = this[t];

                if (!(point.X > -1f && point.Y > -1f && point.X < src.Width && point.Y < src.Height))
                {
                    return null;
                }
                else if (hit(src.Surface.GetBilinearSample(point.X, point.Y)))
                {
                    return point;
                }
            }
        }

        public float TraceEdge(Rectangle bounds, bool backwards = false)
        {
            // Conditions flip if backwards
            int x = Direction.Width > 0 ^ backwards ? bounds.Right - 1 : bounds.Left;
            int y = Direction.Height > 0 ^ backwards ? bounds.Bottom - 1 : bounds.Top;

            float tx = IntersectX(x);
            float ty = IntersectY(y);

            // Min when backwards == false and Max when backwards == true
            return tx < ty ^ backwards ? tx : ty;
        }

        private float IntersectX(float x)
        {
            // 0.0/0.0 == NaN, in this case the ray is already touching
            float dx = x - Origin.X;
            return dx == 0 ? 0 : dx / Direction.Width;
        }

        private float IntersectY(float y)
        {
            // 0.0/0.0 == NaN, in this case the ray is already touching
            float dy = y - Origin.Y;
            return dy == 0 ? 0 : dy / Direction.Height;
        }
    }
}
