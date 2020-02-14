using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.GrowAndShrink
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class GrowAndShrink : PropertyBasedEffect
    {
        private int radius;
        private ColorBgra color;
        private SmoothingMode smoothingMode;

        public GrowAndShrink() : base(
                typeof(GrowAndShrink).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(GrowAndShrink), "icon.png"),
                SubmenuNames.Distort,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(nameof(radius), PropertyControlType.Slider);
            configUI.SetPropertyControlValue(nameof(radius), ControlInfoPropertyNames.DisplayName, "Radius");

            configUI.SetPropertyControlType(nameof(color),
                PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(color),
                ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType(nameof(smoothingMode), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.Description, "Antialiasing");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(new Int32Property(nameof(radius), 0, -50, 50));
            props.Add(new Int32Property(nameof(color),
                ColorBgra.ToOpaqueInt32(primaryColor.NewAlpha(255)),
                0x000000, 0xffffff));

            props.Add(new BooleanProperty(nameof(smoothingMode), false));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(GrowAndShrink).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            radius = newToken.GetProperty<Int32Property>(nameof(radius)).Value;
            color = ColorBgra.FromOpaqueInt32(
                newToken.GetProperty<Int32Property>(nameof(color)).Value);
            smoothingMode = newToken.GetProperty<BooleanProperty>(nameof(smoothingMode)).Value
                ? SmoothingMode.AntiAlias : SmoothingMode.Default;
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
            if(radius == 0)
            {
                dst.CopySurface(src, rect.Location, rect);
                return;
            }

            for(int y = rect.Top; y < rect.Bottom; y++)
            {
                if(IsCancelRequested) { return; }
                for(int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra blendedColor = ColorBgra.Blend(color, src[x, y], src[x, y].A);
                    byte alpha = kernel.WeightedExtremeAlpha(src, x, y, radius < 0);

                    dst[x, y] = blendedColor.NewAlpha(alpha);
                }
            }
        }

        private Kernel GetKernel()
        {
            int size = Math.Abs(radius)*2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = smoothingMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillEllipse(Brushes.Black, 0, 0, bitmap.Width, bitmap.Height);
            return new Kernel(bitmap);
        }
    }
}
