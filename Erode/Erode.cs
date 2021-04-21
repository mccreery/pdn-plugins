using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class Erode : PropertyBasedEffect
    {
        public enum PropertyName
        {
            Radius
        }

        private int radius;

        public Erode() : base(
                typeof(Erode).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(Erode), "icon.png"),
                "Object",
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyName.Radius, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.Radius, ControlInfoPropertyNames.DisplayName, "Radius");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(new Int32Property(PropertyName.Radius, 5, 0, 50));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(Erode).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            radius = newToken.GetProperty<Int32Property>(PropertyName.Radius).Value;
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Kernel kernel = Kernel.CreateCircle(Math.Abs(radius));
            int endIndex = startIndex + length;

            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i], kernel);
            }
        }

        private void Render(Surface dst, Surface src, Rectangle rect, Kernel kernel)
        {
            if (radius == 0)
            {
                dst.CopySurface(src, rect.Location, rect);
                return;
            }

            BitMask mask = GetMask(src, rect, kernel);

            foreach ((Point point, bool marked) in mask)
            {
                if (IsCancelRequested)
                {
                    break;
                }

                ColorBgra srcColor = src[point];

                if (marked && srcColor.A > 0)
                {
                    byte alpha = kernel.ExtremeAlpha(src, point, true);
                    dst[point] = srcColor.NewAlpha(ByteUtil.FastScale(srcColor.A, alpha));
                }
                else
                {
                    dst[point] = srcColor;
                }
            }
        }

        private BitMask GetMask(Surface src, Rectangle rect, Kernel kernel)
        {
            BitMask mask = new BitMask(rect);
            Rectangle influence = rect.Add(kernel.Bounds);
            influence.Intersect(src.Bounds);

            Point point = new Point();
            for (point.Y = influence.Top; point.Y < influence.Bottom; point.Y++)
            {
                for (point.X = influence.Left; point.X < influence.Right; point.X++)
                {
                    byte a = src[point].A;
                    if (a < 255)
                    {
                        Rectangle markedRect = kernel.Bounds;
                        markedRect.Offset(point);
                        mask.MarkRect(markedRect);
                    }
                }
            }
            return mask;
        }
    }

    public enum Method
    {
        Neighborhood,
        EdgeDetection
    }
}
