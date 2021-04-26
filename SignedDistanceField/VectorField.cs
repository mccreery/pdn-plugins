using System.Drawing;

namespace AssortedPlugins.SignedDistanceField
{
    class VectorField
    {
        public Size Size { get; }
        public int Width => Size.Width;
        public int Height => Size.Height;

        readonly int boundaryWidth;
        readonly SizeF[,] data;

        public VectorField(Size size, int boundaryWidth, SizeF boundaryValue)
        {
            Size = size;
            this.boundaryWidth = boundaryWidth;

            int dataWidth = size.Width + boundaryWidth * 2;
            int dataHeight = size.Height + boundaryWidth * 2;
            data = new SizeF[dataHeight, dataWidth];

            for (int y = 0; y < dataHeight; y++)
            {
                for (int x = 0; x < dataWidth; x++)
                {
                    data[y, x] = boundaryValue;
                }
            }
        }

        public SizeF this[Point position]
        {
            get => data[position.Y + boundaryWidth, position.X + boundaryWidth];
            set => data[position.Y + boundaryWidth, position.X + boundaryWidth] = value;
        }
    }
}
