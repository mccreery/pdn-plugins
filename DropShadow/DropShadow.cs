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
        private Color color;
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
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            //RenderingKernels.GaussianBlur(GetBitmapData(DstArgs.Surface), GetBitmapData(SrcArgs.Surface), renderRects, startIndex, length, outlineWidth);

            Kernel kernel = GetKernel();
            int endIndex = startIndex + length;

            for(int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i], kernel);
            }
        }

        private void Render(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            /*if(outlineWidth == 0)
            {
                dst.CopySurface(src, rect.Location, rect);
                return;
            }*/

            for(int y = rect.Top; y < rect.Bottom; y++)
            {
                if(IsCancelRequested) { return; }
                for(int x = rect.Left; x < rect.Right; x++)
                {
                    byte maxAlpha = kernel.WeightedMaxAlpha(src, x, y);
                    byte multipliedAlpha = (byte)Math.Round(255/*outlineColor.A*/ * (maxAlpha / 255.0));

                    ColorBgra color = ColorBgra.Black/*outlineColor*/.NewAlpha(multipliedAlpha);
                    dst[x, y] = UserBlendOps.NormalBlendOp.ApplyStatic(color, src[x, y]);
                }
            }
        }

        private Kernel GetKernel()
        {
            int size = Math.Abs(10/*outlineWidth*/)*2 + 1;

            Bitmap bitmap = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(bitmap);

            g.SmoothingMode = SmoothingMode.AntiAlias/*smoothingMode*/;
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
