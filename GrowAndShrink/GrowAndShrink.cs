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
        private Method method;
        private int radius;
        private ColorBgra outlineColor;
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

            configUI.SetPropertyControlType(nameof(method), PropertyControlType.DropDown);
            configUI.SetPropertyControlValue(nameof(method), ControlInfoPropertyNames.DisplayName, "Method");

            PropertyControlInfo methodControl = configUI.FindControlForPropertyName(nameof(method));
            methodControl.SetValueDisplayName(Method.EdgeDetection, "Edge Detection (faster)");
            methodControl.SetValueDisplayName(Method.Neighborhood, "Neighborhood (slower)");

            configUI.SetPropertyControlType(nameof(radius), PropertyControlType.Slider);
            configUI.SetPropertyControlValue(nameof(radius), ControlInfoPropertyNames.DisplayName, "Radius");

            configUI.SetPropertyControlType(nameof(outlineColor),
                PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(outlineColor),
                ControlInfoPropertyNames.DisplayName, "Outline Color");

            configUI.SetPropertyControlType(nameof(smoothingMode), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.Description, "Antialiasing");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(StaticListChoiceProperty.CreateForEnum<Method>(nameof(method), Method.EdgeDetection, false));
            props.Add(new Int32Property(nameof(radius), 0, -50, 50));
            props.Add(new Int32Property(nameof(outlineColor), (int)(uint)EnvironmentParameters.PrimaryColor));

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

            method = (Method)newToken.GetProperty<StaticListChoiceProperty>(nameof(method)).Value;
            radius = newToken.GetProperty<Int32Property>(nameof(radius)).Value;
            outlineColor = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(nameof(outlineColor)).Value;
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
                    byte maxAlpha = kernel.WeightedExtremeAlpha(src, x, y, radius < 0);
                    byte multipliedAlpha = (byte)Math.Round(outlineColor.A * (maxAlpha / 255.0));

                    ColorBgra color = outlineColor.NewAlpha(multipliedAlpha);
                    dst[x, y] = UserBlendOps.NormalBlendOp.ApplyStatic(color, src[x, y]);
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

    public enum Method
    {
        Neighborhood,
        EdgeDetection
    }
}
