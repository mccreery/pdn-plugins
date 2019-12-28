using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace AssortedPlugins.GrowAndShrink
{
    class BinaryMask : IEnumerable<Point>
    {
        private readonly LinkedList<int> rle = new LinkedList<int>();

        private Size _size;
        public Size Size
        {
            get => _size;
            set
            {
                throw new NotImplementedException();
            }
        }

        public BinaryMask(Size size)
        {
            _size = size;
        }

        public BinaryMask(BinaryMask mask) : this(mask.Size)
        {
            foreach (int runLength in mask.rle)
            {
                rle.AddLast(runLength);
            }
        }

        public BinaryMask(Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                _size = Size.Empty;
            }
            else
            {
                EnsureNonNegativeLocation(rect);
                _size = new Size(rect.Right, rect.Bottom);

                rle.AddLast(rect.Y * Size.Width + rect.X);

                if (rect.X == 0)
                {
                    rle.AddLast(rect.Height * Size.Width);
                }
                else
                {
                    rle.AddLast(rect.Width);
                    for (int i = 1; i < rect.Height; i++)
                    {
                        rle.AddLast(rect.Left);
                        rle.AddLast(rect.Width);
                    }
                }
            }
        }

        public void Resize(Size size)
        {
            throw new NotImplementedException();
        }

        public void Complement()
        {
            if (rle.First.Value == 0)
            {
                rle.RemoveFirst();
            }
            else
            {
                rle.AddFirst(0);
            }
        }

        public void Include(BinaryMask mask)
        {
            // De Morgan's law: A | B == ~(~A & ~B)
            Complement();
            Filter(~mask);
            Complement();
        }

        public void Exclude(BinaryMask mask)
        {
            // A \ B == A & ~B
            Filter(~mask);
        }

        public BinaryMask Filter(BinaryMask mask)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Point> GetEnumerator()
        {
            Point point = new Point();
            bool bit = false;

            foreach (int runLength in rle)
            {
                if (bit)
                {
                    // Step through run one-by-one
                    for (int i = 0; i < runLength; i++)
                    {
                        // Make sure caller doesn't modify our local point
                        yield return new Point(point.X, point.Y);
                        Advance(point, 1);
                    }
                }
                else
                {
                    Advance(point, runLength);
                }
                bit = !bit;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Point point)
        {
            if (new Rectangle(Point.Empty, Size).Contains(point))
            {
                bool bit = false;
                Point cursor = new Point();

                foreach (int runLength in rle)
                {
                    Advance(cursor, runLength);
                    if (cursor.Y > point.Y || (cursor.Y == point.Y && cursor.X > point.X))
                    {
                        return bit;
                    }
                    bit = !bit;
                }
            }
            return false;
        }

        private void Advance(Point point, int runLength)
        {
            point.X += runLength;
            while (point.X >= Size.Width)
            {
                point.X -= Size.Width;
                ++point.Y;
            }
        }

        public static BinaryMask operator ~(BinaryMask mask)
        {
            mask = new BinaryMask(mask);
            mask.Complement();
            return mask;
        }

        public static BinaryMask operator |(BinaryMask a, BinaryMask b)
        {
            BinaryMask mask = new BinaryMask(a);
            mask.Include(b);
            return mask;
        }

        public static BinaryMask operator &(BinaryMask a, BinaryMask b)
        {
            BinaryMask mask = new BinaryMask(a);
            mask.Filter(b);
            return mask;
        }

        public static explicit operator BinaryMask(Rectangle rect) => new BinaryMask(rect);

        private static void EnsureNonNegativeLocation(Rectangle rect)
        {
            if (rect.X < 0)
            {
                rect.Width += rect.X;
                rect.X = 0;
            }

            if (rect.Y < 0)
            {
                rect.Height += rect.Y;
                rect.Y = 0;
            }
        }
    }
}
