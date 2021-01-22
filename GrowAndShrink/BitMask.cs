using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace AssortedPlugins
{
    public class BitMask : IEnumerable<(Point, bool)>
    {
        public Rectangle Bounds { get; }

        private byte[][] data;

        public BitMask(Rectangle bounds)
        {
            Bounds = bounds;

            data = new byte[bounds.Height][];
            int rowLength = (bounds.Width + 7) >> 3;

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new byte[rowLength];
            }
        }

        public bool this[int x, int y]
        {
            get
            {
                x -= Bounds.Left;
                y -= Bounds.Top;

                return (data[y][x >> 3] & Bit(x & 7)) != 0;
            }
        }
        public bool this[Point point] => this[point.X, point.Y];

        public IEnumerator<(Point, bool)> GetEnumerator()
        {
            for (int y = Bounds.Top; y < Bounds.Bottom; y++)
            {
                byte[] row = data[y - Bounds.Top];

                for (int i = 0, x = Bounds.Left; x < Bounds.Right; i++)
                {
                    byte chunk = row[i];
                    int n = Math.Min(Bounds.Right - x, 8);

                    for (int j = 0; j < n; j++, x++)
                    {
                        yield return (new Point(x, y), (chunk & Bit(j)) != 0);
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void MarkRect(Rectangle rect)
        {
            rect.Intersect(Bounds);
            rect.Location -= (Size)Bounds.Location;

            int loByte;
            byte[] rowData = GetRowData(rect.Left, rect.Right, out loByte);

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                byte[] row = data[y];
                for (int i = 0; i < rowData.Length; i++)
                {
                    row[loByte + i] |= rowData[i];
                }
            }
        }

        private static byte[] GetRowData(int left, int right, out int loByte)
        {
            loByte = left >> 3;
            if (left >= right)
            {
                return new byte[0];
            }
            int hiByte = (right - 1) >> 3;

            byte[] rowData = new byte[hiByte - loByte + 1];
            for (int i = 0; i < rowData.Length; i++)
            {
                rowData[i] = 0xff;
            }

            if ((right & 7) != 0)
            {
                rowData[rowData.Length - 1] = LoBits(right & 7);
            }
            rowData[0] ^= LoBits(left & 7);

            return rowData;
        }

        private static byte Bit(int n) => (byte)(1 << n);
        private static byte LoBits(int n) => (byte)(Bit(n) - 1);
    }
}
