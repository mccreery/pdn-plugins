using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Direct2D1.Effects;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;

namespace AssortedPlugins.Recolor
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public class Recolor : PropertyBasedEffect
    {
        public enum PropertyName
        {
            Color,
            Flat,
            Gamma
        }

        private ColorBgra targetColor;
        private bool flat;
        private double gamma;

        public Recolor() : base(
                typeof(Recolor).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(Recolor), "icon.png"),
                null,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyName.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.Color, ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType(PropertyName.Flat, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.Flat, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyName.Flat, ControlInfoPropertyNames.Description, "Flat");

            configUI.SetPropertyControlType(PropertyName.Gamma, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyName.Gamma, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyName.Gamma, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyName.Gamma, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyName.Gamma, ControlInfoPropertyNames.DisplayName, "Gamma");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyName.Color, (int)(uint)EnvironmentParameters.PrimaryColor));
            props.Add(new BooleanProperty(PropertyName.Flat, false));
            props.Add(new DoubleProperty(PropertyName.Gamma, 1, 0, 1));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(Recolor).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            targetColor = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(PropertyName.Color).Value;
            flat = newToken.GetProperty<BooleanProperty>(PropertyName.Flat).Value;
            gamma = newToken.GetProperty<DoubleProperty>(PropertyName.Gamma).Value;

            FindMaxComponents();
            CacheGammaCorrection();
        }

        private byte maxValue;
        private byte maxAlpha;

        /// <summary>
        ///   Populates <see cref="maxValue"/> and <see cref="maxAlpha"/> from the source image.
        /// </summary>
        private void FindMaxComponents()
        {
            maxValue = 0;
            maxAlpha = 0;

            for (int y = 0; y < SrcArgs.Height; y++)
            {
                for (int x = 0; x < SrcArgs.Width; x++)
                {
                    ColorBgra srcColor = SrcArgs.Surface[x, y];
                    // Calculate value from components
                    byte value = Value(srcColor);

                    maxValue = Math.Max(maxValue, value);
                    maxAlpha = Math.Max(maxAlpha, srcColor.A);
                }
            }
        }

        private byte[] gammaLookup = new byte[256];

        /// <summary>
        ///   Populates <see cref="gammaLookup"/> if gamma != 1.0.
        /// </summary>
        private void CacheGammaCorrection()
        {
            if (gamma != 1.0)
            {
                for (int i = 0; i < 256; i++)
                {
                    double valueD = ByteUtil.ToScalingFloat((byte)i);
                    valueD = Math.Pow(valueD, gamma) * 255.0;
                    // Round and cast in one without considering negative values
                    gammaLookup[i] = (byte)(valueD + 0.5);
                }
            }
        }

        /// <summary>
        ///   Calculates the value component of a color.
        /// </summary>
        /// <param name="color">The input coor.</param>
        /// <returns>The average brightness over the components of the color.</returns>
        private byte Value(ColorBgra color)
        {
            float average = (color.B + color.G + color.R) / 3.0f;
            // Round and cast in one without considering negative values
            return (byte)(average + 0.5f);
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

                    // Saturate alpha
                    byte alpha = ByteUtil.FastUnscale(srcColor.A, maxAlpha);
                    // Rescale to target alpha
                    alpha = ByteUtil.FastScale(alpha, targetColor.A);

                    // Where maxValue == 0, the image is all black
                    if (flat || maxValue == 0)
                    {
                        dst[x, y] = targetColor.NewAlpha(alpha);
                    }
                    else
                    {
                        // Calculate value from components
                        byte value = Value(srcColor);
                        // Saturate value
                        value = ByteUtil.FastUnscale(value, maxValue);

                        if (gamma != 1.0)
                        {
                            value = gammaLookup[value];
                        }

                        // Rescale to target color and alpha
                        dst[x, y] = ColorBgra.FromBgra(
                            ByteUtil.FastScale(value, targetColor.B),
                            ByteUtil.FastScale(value, targetColor.G),
                            ByteUtil.FastScale(value, targetColor.R),
                            ByteUtil.FastScale(alpha, targetColor.A));
                    }
                }
            }
        }
    }
}
