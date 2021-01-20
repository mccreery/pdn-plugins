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
    public class DropShadow : MultipassEffect
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

        protected override RenderPhase[] Phases
        {
            get
            {
                List<RenderPhase> phases = new List<RenderPhase>();
                phases.Add(RenderSilhouette);

                if (spreadRadius > 0)
                {
                    phases.Add(RenderSpread);
                }
                if (blurRadius > 0)
                {
                    phases.Add(RenderBlur);
                }
                if (!shadowOnly)
                {
                    phases.Add(RenderBlendOver);
                }

                return phases.ToArray();
            }
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

            // Generate kernel once instead of per rectangle
            kernel = GetKernel();
        }

        private void RenderBlur(RenderArgs dst, RenderArgs src, Rectangle rect)
        {
            RenderingKernels.GaussianBlur(GetBitmapData(dst.Surface), GetBitmapData(src.Surface), new Rectangle[] { rect }, 0, 1, blurRadius);
        }

        private Kernel kernel;

        /// <summary>
        ///   Fills a rectangle with a solid color, applying spread to the alpha channel.
        /// </summary>
        private void RenderSpread(RenderArgs dst, RenderArgs src, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    byte maxAlpha = kernel.WeightedMaxAlpha(src.Surface, new Point(x, y));
                    dst.Surface[x, y] = MultiplyAlpha(color, maxAlpha);
                }
            }
        }

        private static ColorBgra MultiplyAlpha(ColorBgra color, byte alpha)
        {
            return color.NewAlpha((byte)Math.Round(color.A / 255.0 * alpha));
        }

        private void RenderBlendOver(RenderArgs dst, RenderArgs src, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst.Surface[x, y] = UserBlendOps.NormalBlendOp.ApplyStatic(src.Surface[x, y], SrcArgs.Surface[x, y]);
                }
            }
        }

        /// <summary>
        ///   A single render pass which renders the shifted silhouette.
        /// </summary>
        private void RenderSilhouette(RenderArgs dst, RenderArgs src, Rectangle rect)
        {
            dst.Surface.CopySurface(src.Surface, rect, new Size(offsetX, offsetY));

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    byte alpha = dst.Surface[x, y].A;
                    if (inset) alpha = (byte)(255 - alpha);

                    dst.Surface[x, y] = MultiplyAlpha(color, alpha);
                }
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
