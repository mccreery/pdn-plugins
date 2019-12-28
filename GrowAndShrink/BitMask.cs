using System;
using System.Collections.Generic;
using System.Drawing;

namespace AssortedPlugins
{
    class BitMask
    {
        private BitShiftCache bitShiftCache;
        public Size Size { get; }

        // Masks for the end of the row
        private readonly int maskRowIndex;
        private readonly byte mask;

        public BitMask(Size size)
        {
            Size = size;
            // Round up byte width and add padding byte for shift
            byte[,] data = new byte[size.Height, ((size.Width + 7) >> 3) + 1];
            bitShiftCache = new BitShiftCache(data);

            maskRowIndex = data.GetLength(1) - 2;
            mask = MaskTopBits(size.Width & 7);
        }

        public IEnumerator<bool> GetEnumerator()
        {
            byte[,] data = bitShiftCache.Data;

            for (int y = 0; y < Size.Height; y++)
            {
                int x, rowIndex;
                for (x = 0, rowIndex = 0; x + 8 <= Size.Width; x += 8, rowIndex++)
                {
                    byte a = data[y, rowIndex];
                    byte mask = (byte)0x80u;

                    for (int i = 0; i < 8; i++, mask >>= 1)
                    {
                        yield return (a & mask) != 0;
                    }
                }
            }
        }

        public void Complement()
        {
            byte[,] data = bitShiftCache.Data;

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i, j] = (byte)~data[i, j];
                }
            }
            bitShiftCache.Invalidate();
            Clamp();
        }

        public void Add(BitMask mask, Point offset)
        {
            byte[,] data = bitShiftCache.Data;
            byte[,] shiftedMaskData = mask.bitShiftCache[offset.X & 7];

            int offsetRowIndex = offset.X >> 3;

            int yMin = Math.Max(offset.Y, 0);
            int yMax = Math.Min(offset.Y + shiftedMaskData.GetLength(0), data.GetLength(0));

            int xMin = Math.Max(offsetRowIndex, 0);
            int xMax = Math.Min(offsetRowIndex + shiftedMaskData.GetLength(1), data.GetLength(1));

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    data[y, x] |= shiftedMaskData[y - offset.Y, x - offsetRowIndex];
                }
            }
            bitShiftCache.Invalidate();
            Clamp();
        }

        private void Clamp()
        {
            if (mask != 0xffu)
            {
                byte[,] data = bitShiftCache.Data;

                for (int i = 0; i < data.GetLength(0); i++)
                {
                    data[i, maskRowIndex] &= mask;
                    for (int j = maskRowIndex + 1; j < data.GetLength(1); j++)
                    {
                        data[i, j] = 0;
                    }
                }
            }
            bitShiftCache.Invalidate();
        }

        private static byte MaskBottomBits(int n)
        {
            return (byte)((1u << n) - 1);
        }

        private static byte MaskTopBits(int n)
        {
            return (byte)~MaskBottomBits(8 - n);
        }
    }
}
