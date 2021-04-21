using System;
using System.Collections.Generic;
using System.Drawing;
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
            Stop1,
            Stop1Color,
            Stop2,
            Stop2Color,
            Stop3,
            Stop3Color,
            IgnoreTransparentPixels
        }

        private int[] stopPositions = new int[3];
        private ColorBgra[] stopColors = new ColorBgra[3];
        private bool ignoreTransparentPixels;

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

            configUI.SetPropertyControlType(PropertyName.Stop1, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.Stop1, ControlInfoPropertyNames.DisplayName, "Stop 1");
            configUI.SetPropertyControlType(PropertyName.Stop1Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.Stop1Color, ControlInfoPropertyNames.DisplayName, "Color 1");

            configUI.SetPropertyControlType(PropertyName.Stop2, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.Stop2, ControlInfoPropertyNames.DisplayName, "Stop 2");
            configUI.SetPropertyControlType(PropertyName.Stop2Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.Stop2Color, ControlInfoPropertyNames.DisplayName, "Color 2");

            configUI.SetPropertyControlType(PropertyName.Stop3, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.Stop3, ControlInfoPropertyNames.DisplayName, "Stop 3");
            configUI.SetPropertyControlType(PropertyName.Stop3Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.Stop3Color, ControlInfoPropertyNames.DisplayName, "Color 3");

            configUI.SetPropertyControlType(PropertyName.IgnoreTransparentPixels, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.IgnoreTransparentPixels, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyName.IgnoreTransparentPixels, ControlInfoPropertyNames.Description, "Ignore transparent pixels");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyName.Stop1, 0, 0, 255));
            props.Add(new Int32Property(PropertyName.Stop1Color, (int)ColorBgra.Red.Bgra));

            props.Add(new Int32Property(PropertyName.Stop2, 127, 0, 255));
            props.Add(new Int32Property(PropertyName.Stop2Color, (int)ColorBgra.Green.Bgra));

            props.Add(new Int32Property(PropertyName.Stop3, 255, 0, 255));
            props.Add(new Int32Property(PropertyName.Stop3Color, (int)ColorBgra.Blue.Bgra));

            props.Add(new BooleanProperty(PropertyName.IgnoreTransparentPixels, true));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(ColorRamp).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            stopPositions[0] = newToken.GetProperty<Int32Property>(PropertyName.Stop1).Value;
            stopColors[0] = ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.Stop1Color).Value);

            stopPositions[1] = newToken.GetProperty<Int32Property>(PropertyName.Stop2).Value;
            stopColors[1] = ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.Stop2Color).Value);

            stopPositions[2] = newToken.GetProperty<Int32Property>(PropertyName.Stop3).Value;
            stopColors[2] = ColorBgra.FromUInt32((uint)newToken.GetProperty<Int32Property>(PropertyName.Stop3Color).Value);

            ignoreTransparentPixels = newToken.GetProperty<BooleanProperty>(PropertyName.IgnoreTransparentPixels).Value;
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
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra srcColor = src[x, y];

                    if (ignoreTransparentPixels && srcColor.A == 0)
                    {
                        dst[x, y] = srcColor;
                        continue;
                    }

                    int brightness = (int)Math.Round((srcColor.R + srcColor.G + srcColor.B) / 3.0);

                    if (brightness > stopPositions[1])
                    {
                        float t = InverseLerp(stopPositions[1], stopPositions[2], brightness);
                        dst[x, y] = Lerp(stopColors[1], stopColors[2], t);
                    }
                    else
                    {
                        float t = InverseLerp(stopPositions[0], stopPositions[1], brightness);
                        dst[x, y] = Lerp(stopColors[0], stopColors[1], t);
                    }
                }
            }
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
