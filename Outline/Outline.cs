using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using static PaintDotNet.UserBlendOps;

namespace AssortedPlugins
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class Outline : PropertyBasedEffect
    {
        private int radius;
        private ColorBgra outlineColor;

        public Outline() : base(
            typeof(Outline).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(Outline), "icon.png"),
            "Object",
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(nameof(radius), PropertyControlType.Slider);
            configUI.SetPropertyControlValue(nameof(radius), ControlInfoPropertyNames.DisplayName, "Radius");

            configUI.SetPropertyControlType(nameof(outlineColor), PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(outlineColor), ControlInfoPropertyNames.DisplayName, "Color");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(new Int32Property(nameof(radius), 0, 0, 50));
            props.Add(new Int32Property(nameof(outlineColor), (int)(uint)EnvironmentParameters.PrimaryColor));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(Outline).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            radius = newToken.GetProperty<Int32Property>(nameof(radius)).Value;
            outlineColor = ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(nameof(outlineColor)).Value);
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Kernel kernel = Kernel.CreateCircle(Math.Abs(radius));
            int endIndex = startIndex + length;

            for(int i = startIndex; i < endIndex; i++)
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

                if (marked && srcColor.A < 255)
                {
                    srcColor = NormalBlendOp.ApplyStatic(outlineColor, srcColor);
                    byte alpha = kernel.ExtremeAlpha(src, point, false);
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
                    if (a > 0)
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
