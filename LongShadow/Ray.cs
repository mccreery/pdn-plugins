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

            if (direction == SizeF.Empty)
            {
                throw new ArgumentException("Direction cannot be zero");
            }

            // Normalize direction
            float length = (float)Math.Sqrt(direction.Width * direction.Width + direction.Height * direction.Height);
            direction.Width /= length;
            direction.Height /= length;
        }

        public Ray Flip()
        {
            return new Ray(Origin, SizeF.Empty - Direction);
        }

        public PointF this[float t] => new PointF(
            Origin.X + Direction.Width * t,
            Origin.Y + Direction.Height * t);

        public PointF? Trace(RectangleF bounds, Predicate<PointF> hit, float t)
        {
            for (; ; t += 1.0f)
            {
                PointF point = this[t];

                if (!bounds.Contains(point))
                {
                    return null;
                }
                else if (hit(point))
                {
                    return point;
                }
            }
        }

        public float TraceEdge(RectangleF bounds)
        {
            float x = Direction.Width > 0 ? bounds.Right : bounds.Left;
            float y = Direction.Height > 0 ? bounds.Bottom : bounds.Top;

            // Rays parallel to horizontal or vertical edges will not touch them
            // In these cases only consider the other edges
            if (Direction.Width == 0)
            {
                return IntersectY(y);
            }
            else if (Direction.Height == 0)
            {
                return IntersectX(x);
            }
            else
            {
                // Consider both edges, the lowest should touch the boundary
                return Math.Min(IntersectX(x), IntersectY(y));
            }
        }

        private float IntersectX(float x)
        {
            if (Direction.Width == 0)
            {
                throw new InvalidOperationException("None or infinite solutions");
            }

            return (x - Origin.X) / Direction.Width;
        }

        private float IntersectY(float y)
        {
            if (Direction.Height == 0)
            {
                throw new InvalidOperationException("None or infinite solutions");
            }

            return (y - Origin.Y) / Direction.Height;
        }
    }
}
