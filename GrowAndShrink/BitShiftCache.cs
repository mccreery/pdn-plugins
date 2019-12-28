using System;

namespace AssortedPlugins
{
    /**
     * <summary>Stores a <c>byte[,]</c> and caches bit shifted copies of it.
     * Cached copies will be inaccurate when the original data is modified.
     * <see cref="Invalidate"/> clears the cache in this case.
     * "Bit shift" in this context means bit shifting each byte with carry into the next byte on the row.
     * Only right shifts 0-7 are supported.</summary>
     */
    public class BitShiftCache
    {
        private readonly byte[][,] shiftedDataCache = new byte[8][,];
        // Temporary flag to indicate the cache is invalid. Cleared when the cache is cleared.
        private bool dirty;

        /**
         * <param name="data"><see cref="Data"/></param>
         */
        public BitShiftCache(byte[,] data)
        {
            shiftedDataCache[0] = data;
        }

        /**
         * <value>The original data. Use <see cref="Invalidate"/> when this changes.</value>
         */
        public byte[,] Data
        {
            get => shiftedDataCache[0];
        }

        /**
         * <summary>Attempts to use a cached bit shifted copy, and creates one if it fails.</summary>
         * <param name="i">The desired bit shift right.</param>
         * <returns>A cached or new shifted copy of the original data.</returns>
         */
        public byte[,] this[int i]
        {
            // Caller can modify both original data and copies
            get
            {
                if (i == 0)
                {
                    return Data;
                }
                else if (dirty)
                {
                    // Validate cache by clearing it
                    for (int j = 1; j < shiftedDataCache.Length; j++)
                    {
                        shiftedDataCache[i] = null;
                    }
                    dirty = false;
                    // Here we know a copy does not exist
                    return shiftedDataCache[i] = CreateShiftedCopy(i);
                }
                else
                {
                    // Cache new copy if it does not exist
                    return shiftedDataCache[i] = shiftedDataCache[i] ?? CreateShiftedCopy(i);
                }
            }
        }

        /**
         * <summary>Clears all cached data copies. Use this if you have modified the original data.</summary>
         */
        public void Invalidate()
        {
            dirty = true;
        }

        private byte[,] CreateShiftedCopy(int shift)
        {
            byte[,] dataCopy = (byte[,])Data.Clone();
            ShiftData(dataCopy, shift);

            return dataCopy;
        }

        private void ShiftData(byte[,] data, int shift)
        {
            if (shift < 0 || shift >= 8)
            {
                throw new ArgumentException("Shift must be between 0 and 7");
            }

            for (int i = 0; i < data.GetLength(0); i++)
            {
                // Shift without carry for last byte
                data[i, data.GetLength(1) - 1] >>= shift;

                // Shift with carry for remaining bytes
                for (int j = data.GetLength(1) - 2; j >= 0; j--)
                {
                    data[i, j + 1] |= (byte)(data[i, j] << (8 - shift));
                    data[i, j] >>= shift;
                }
            }
        }
    }
}
