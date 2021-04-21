using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.ColorRamp
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public class ColorRamp : PropertyBasedEffect
    {
        public enum PropertyName
        {
            LowStop,
            LowStopColor,
            EnableMidStop,
            MidStop,
            MidStopColor,
            EnableHighStop,
            HighStop,
            HighStopColor
        }

        private struct Stop
        {
            public Stop(int position, ColorBgra color)
            {
                Position = position;
                Color = color;
            }

            public int Position { get; }
            public ColorBgra Color { get; }
        }

        private readonly List<Stop> stops = new List<Stop>();

        public ColorRamp() : base(
            typeof(ColorRamp).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(ColorRamp), "icon.png"),
            null,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyName.LowStop, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.LowStop, ControlInfoPropertyNames.DisplayName, "Low Stop");
            configUI.SetPropertyControlType(PropertyName.LowStopColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.LowStopColor, ControlInfoPropertyNames.DisplayName, "Low Stop Color");

            configUI.SetPropertyControlType(PropertyName.EnableMidStop, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.EnableMidStop, ControlInfoPropertyNames.DisplayName, "Mid Stop");
            configUI.SetPropertyControlValue(PropertyName.EnableMidStop, ControlInfoPropertyNames.Description, "Enable Mid Stop");

            configUI.SetPropertyControlType(PropertyName.MidStop, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.MidStop, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlType(PropertyName.MidStopColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.MidStopColor, ControlInfoPropertyNames.DisplayName, "Mid Stop Color");

            configUI.SetPropertyControlType(PropertyName.EnableHighStop, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.EnableHighStop, ControlInfoPropertyNames.DisplayName, "High Stop");
            configUI.SetPropertyControlValue(PropertyName.EnableHighStop, ControlInfoPropertyNames.Description, "Enable High Stop");

            configUI.SetPropertyControlType(PropertyName.HighStop, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.HighStop, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlType(PropertyName.HighStopColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.HighStopColor, ControlInfoPropertyNames.DisplayName, "High Stop Color");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            List<PropertyCollectionRule> rules = new List<PropertyCollectionRule>();

            props.Add(new Int32Property(PropertyName.LowStop, 0, 0, 255));
            props.Add(new Int32Property(PropertyName.LowStopColor, (int)ColorBgra.Black.Bgra));

            props.Add(new BooleanProperty(PropertyName.EnableMidStop, false));
            props.Add(new Int32Property(PropertyName.MidStop, 127, 0, 255));
            props.Add(new Int32Property(PropertyName.MidStopColor, (int)ColorBgra.Red.Bgra));

            rules.Add(new ReadOnlyBoundToBooleanRule(PropertyName.MidStop, PropertyName.EnableMidStop, true));
            rules.Add(new ReadOnlyBoundToBooleanRule(PropertyName.MidStopColor, PropertyName.EnableMidStop, true));

            props.Add(new BooleanProperty(PropertyName.EnableHighStop, true));
            props.Add(new Int32Property(PropertyName.HighStop, 255, 0, 255));
            props.Add(new Int32Property(PropertyName.HighStopColor, (int)ColorBgra.White.Bgra));

            rules.Add(new ReadOnlyBoundToBooleanRule(PropertyName.HighStop, PropertyName.EnableHighStop, true));
            rules.Add(new ReadOnlyBoundToBooleanRule(PropertyName.HighStopColor, PropertyName.EnableHighStop, true));

            return new PropertyCollection(props, rules);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(ColorRamp).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            stops.Clear();

            stops.Add(new Stop(
                newToken.GetProperty<Int32Property>(PropertyName.LowStop).Value,
                ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.LowStopColor).Value)));

            if (newToken.GetProperty<BooleanProperty>(PropertyName.EnableMidStop).Value)
            {
                stops.Add(new Stop(
                    newToken.GetProperty<Int32Property>(PropertyName.MidStop).Value,
                    ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.MidStopColor).Value)));
            }

            if (newToken.GetProperty<BooleanProperty>(PropertyName.EnableHighStop).Value)
            {
                stops.Add(new Stop(
                    newToken.GetProperty<Int32Property>(PropertyName.HighStop).Value,
                    ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.HighStopColor).Value)));
            }
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            int endIndex = startIndex + length;

            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            List<Stop> orderedStops = GetStops();
            ColorBgra[] lut = new ColorBgra[256];

            for (int stopIndex = 0; stopIndex < orderedStops.Count - 1; stopIndex++)
            {
                Stop startStop = orderedStops[stopIndex];
                Stop endStop = orderedStops[stopIndex + 1];

                for (int x = startStop.Position; x <= endStop.Position; x++)
                {
                    float t = InverseLerp(startStop.Position, endStop.Position, x);
                    lut[x] = Lerp(startStop.Color, endStop.Color, t);
                }
            }

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra srcColor = src[x, y];
                    int brightness = (int)Math.Round((srcColor.R + srcColor.G + srcColor.B) / 3.0);

                    ColorBgra dstColor = lut[brightness];
                    dstColor.A = ByteUtil.FastScale(srcColor.A, dstColor.A);

                    dst[x, y] = dstColor;
                }
            }
        }

        private List<Stop> GetStops()
        {
            List<Stop> orderedStops = stops
                .OrderBy(stop => stop.Position)
                .GroupBy(stop => stop.Position)
                .Select(g => g.First())
                .ToList();

            if (orderedStops.First().Position > 0)
            {
                orderedStops.Insert(0, new Stop(0, orderedStops.First().Color));
            }
            if (orderedStops.Last().Position < 255)
            {
                orderedStops.Add(new Stop(255, orderedStops.Last().Color));
            }
            return orderedStops;
        }

        private static float Lerp(float a, float b, float t)
        {
            return t * b + (1.0f - t) * a;
        }

        private static ColorBgra Lerp(ColorBgra a, ColorBgra b, float t)
        {
            float blue = Lerp(a.B, b.B, t);
            float green = Lerp(a.G, b.G, t);
            float red = Lerp(a.R, b.R, t);
            float alpha = Lerp(a.A, b.A, t);

            return ColorBgra.FromBgraClamped(blue, green, red, alpha);
        }

        private static float InverseLerp(float a, float b, float x)
        {
            return (x - a) / (float)(b - a);
        }
    }
}
