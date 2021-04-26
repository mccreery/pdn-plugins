using System.Drawing;

namespace AssortedPlugins.SignedDistanceField
{
    class VectorField
    {
        public Size Size { get; }
        public int Width => Size.Width;
        public int Height => Size.Height;

        readonly SizeF[,] data;

        public VectorField(Size size)
        {
            Size = size;
            data = new SizeF[size.Height, size.Width];
        }

        public SizeF this[Point position]
        {
            get
            {
                if (new Rectangle(Point.Empty, Size).Contains(position))
                {
                    return data[position.Y, position.X];
                }
                else
                {
                    return new SizeF(float.PositiveInfinity, float.PositiveInfinity);
                }
            }
            set => data[position.Y, position.X] = value;
        }
    }
}
