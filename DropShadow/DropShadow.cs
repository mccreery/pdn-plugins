using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.SystemLayer;

namespace AssortedPlugins.DropShadow
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class DropShadow : PropertyBasedEffect
    {
        private ColorBgra color;
        private int offsetX;
        private int offsetY;
        private int spreadRadius;
        private int blurRadius;
        private bool inset;
        private bool shadowOnly;

        public DropShadow() : base(
                typeof(DropShadow).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(DropShadow), "icon.png"),
                SubmenuNames.Distort,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Color, ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType(PropertyNames.OffsetX, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.OffsetX, ControlInfoPropertyNames.DisplayName, "Horizontal Offset");
            configUI.SetPropertyControlType(PropertyNames.OffsetY, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.OffsetY, ControlInfoPropertyNames.DisplayName, "Vertical Offset");

            configUI.SetPropertyControlType(PropertyNames.SpreadRadius, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.SpreadRadius, ControlInfoPropertyNames.DisplayName, "Spread Radius");
            configUI.SetPropertyControlType(PropertyNames.BlurRadius, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.BlurRadius, ControlInfoPropertyNames.DisplayName, "Blur Radius");

            configUI.SetPropertyControlType(PropertyNames.Inset, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyNames.Inset, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.Inset, ControlInfoPropertyNames.Description, "Inset");

            configUI.SetPropertyControlType(PropertyNames.ShadowOnly, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyNames.ShadowOnly, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.ShadowOnly, ControlInfoPropertyNames.Description, "Shadow Only");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Color, (int)(uint)EnvironmentParameters.PrimaryColor));
            props.Add(new Int32Property(PropertyNames.OffsetX, 0, -100, 100));
            props.Add(new Int32Property(PropertyNames.OffsetY, 0, -100, 100));
            props.Add(new Int32Property(PropertyNames.SpreadRadius, 0, 0, 100));
            props.Add(new Int32Property(PropertyNames.BlurRadius, 0, 0, 100));
            props.Add(new BooleanProperty(PropertyNames.Inset, false));
            props.Add(new BooleanProperty(PropertyNames.ShadowOnly, false));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(DropShadow).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        private Surface shifted;

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            color = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(PropertyNames.Color).Value;
            offsetX = newToken.GetProperty<Int32Property>(PropertyNames.OffsetX).Value;
            offsetY = newToken.GetProperty<Int32Property>(PropertyNames.OffsetY).Value;
            spreadRadius = newToken.GetProperty<Int32Property>(PropertyNames.SpreadRadius).Value;
            blurRadius = newToken.GetProperty<Int32Property>(PropertyNames.BlurRadius).Value;
            inset = newToken.GetProperty<BooleanProperty>(PropertyNames.Inset).Value;
            shadowOnly = newToken.GetProperty<BooleanProperty>(PropertyNames.ShadowOnly).Value;

            // Prepare offscreen surfaces for each step of the calculation
            shifted = new Surface(srcArgs.Size);
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Kernel kernel = GetKernel();
            int endIndex = startIndex + length;

            for(int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i], kernel);
            }
        }

        private void Render(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            CopyShiftedRect(shifted, src, rect);

            if (spreadRadius == 0)
            {
                Recolor(dst, shifted, rect);
            }
            else
            {
                Spread(dst, shifted, rect, kernel);
            }

            if (!shadowOnly)
            {
                BlendOver(dst, src, rect);
            }
        }

        /// <summary>
        ///   Fills a rectangle with a solid color without changing the alpha, creating a silhouette effect.
        /// </summary>
        private void Recolor(Surface dst, Surface src, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = MultiplyAlpha(color, src[x, y].A);
                }
            }
        }

        /// <summary>
        ///   Fills a rectangle with a solid color, applying spread to the alpha channel.
        /// </summary>
        private void Spread(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    byte maxAlpha = kernel.WeightedMaxAlpha(src, x, y);
                    dst[x, y] = MultiplyAlpha(color, maxAlpha);
                }
            }
        }

        private static ColorBgra MultiplyAlpha(ColorBgra color, byte alpha)
        {
            return color.NewAlpha((byte)Math.Round(color.A / 255.0 * alpha));
        }

        private void BlendOver(Surface dst, Surface src, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = UserBlendOps.NormalBlendOp.ApplyStatic(dst[x, y], src[x, y]);
                }
            }
        }

        /// <summary>
        ///   Copies the shifted image within the destination rectangle only.
        /// </summary>
        private void CopyShiftedRect(Surface dst, Surface src, Rectangle dstRect)
        {
            dst.Clear(dstRect, ColorBgra.Transparent);

            Rectangle srcRect = dstRect;
            srcRect.X -= offsetX;
            srcRect.Y -= offsetY;

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

            if (srcRect.Width > 0 && srcRect.Height > 0)
            {
                dst.CopySurface(src, dstRect.Location, srcRect);
            }
        }

        private Kernel GetKernel()
        {
            int size = Math.Abs(spreadRadius)*2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillEllipse(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height);
            return new Kernel(bitmap);
        }

        private static BitmapData GetBitmapData(Surface surface)
        {
            BitmapData bitmapData = new BitmapData();
            bitmapData.Width = surface.Width;
            bitmapData.Height = surface.Height;
            bitmapData.Stride = surface.Stride;
            bitmapData.Scan0 = surface.Scan0.Pointer;
            return bitmapData;
        }

        public enum PropertyNames
        {
            Color,
            OffsetX,
            OffsetY,
            SpreadRadius,
            BlurRadius,
            Inset,
            ShadowOnly
        }
    }
}
