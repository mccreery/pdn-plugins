using PaintDotNet;
using System.Drawing;

namespace AssortedPlugins
{
    public static class SurfaceExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="dstRect"></param>
        /// <param name="offset"></param>
        public static void CopySurface(this Surface dst, Surface src, Rectangle dstRect, Size offset)
        {
            Rectangle srcRect = dstRect;
            srcRect.Location -= offset;

            if (srcRect.X < 0)
            {
                dstRect.X -= srcRect.X;
                srcRect.Width += srcRect.X;
                srcRect.X = 0;
            }
            else if (srcRect.Right > dst.Width)
            {
                srcRect.Width -= srcRect.Right - dst.Width;
            }

            if (srcRect.Y < 0)
            {
                dstRect.Y -= srcRect.Y;
                srcRect.Height += srcRect.Y;
                srcRect.Y = 0;
            }
            else if (srcRect.Bottom > dst.Height)
            {
                srcRect.Height -= srcRect.Bottom - dst.Height;
            }

            if (srcRect.Size != dstRect.Size)
            {
                dst.Clear(dstRect, ColorBgra.Transparent);
            }

            if (srcRect.Width > 0 && srcRect.Height > 0)
            {
                dst.CopySurface(src, dstRect.Location, srcRect);
            }
        }
    }
}
