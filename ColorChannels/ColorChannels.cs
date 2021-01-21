using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.ColorChannels
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public class ColorChannels : PropertyBasedEffect
    {
        private readonly int[] offsets = new int[4];
        private AlphaOp alphaOp;

        public enum PropertyName
        {
            RedOffset,
            GreenOffset,
            BlueOffset,
            AlphaOffset,
            AlphaOp
        }

        public enum AlphaOp
        {
            Add,
            Multiply
        }

        // Maps RGBA onto BGRA
        private static readonly int[] CHANNEL_MAP = new int[] { 2, 1, 0, 3 };

        public ColorChannels() : base(
            typeof(ColorChannels).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(ColorChannels), "icon.png"),
            null,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyName.RedOffset, ControlInfoPropertyNames.ControlColors, new ColorBgra[] { ColorBgra.FromBgr(255, 255, 0), ColorBgra.White, ColorBgra.FromBgr(0, 0, 255) });
            configUI.SetPropertyControlValue(PropertyName.RedOffset, ControlInfoPropertyNames.DisplayName, "Red");

            configUI.SetPropertyControlValue(PropertyName.GreenOffset, ControlInfoPropertyNames.ControlColors, new ColorBgra[] { ColorBgra.FromBgr(255, 0, 255), ColorBgra.White, ColorBgra.FromBgr(0, 255, 0) });
            configUI.SetPropertyControlValue(PropertyName.GreenOffset, ControlInfoPropertyNames.DisplayName, "Green");

            configUI.SetPropertyControlValue(PropertyName.BlueOffset, ControlInfoPropertyNames.ControlColors, new ColorBgra[] { ColorBgra.FromBgr(0, 255, 255), ColorBgra.White, ColorBgra.FromBgr(255, 0, 0) });
            configUI.SetPropertyControlValue(PropertyName.BlueOffset, ControlInfoPropertyNames.DisplayName, "Blue");

            configUI.SetPropertyControlValue(PropertyName.AlphaOffset, ControlInfoPropertyNames.ControlColors, new ColorBgra[] { ColorBgra.Black, ColorBgra.Black, ColorBgra.White });
            configUI.SetPropertyControlValue(PropertyName.AlphaOffset, ControlInfoPropertyNames.DisplayName, "Alpha");

            configUI.SetPropertyControlType(PropertyName.AlphaOp, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.AlphaOp, ControlInfoPropertyNames.DisplayName, "Alpha Operation");
            configUI.SetPropertyControlValue(PropertyName.AlphaOp, ControlInfoPropertyNames.Description, "Using the Multiply operation, -255 corresponds to 0% and +255 corresponds to 200% alpha.");

            PropertyControlInfo Amount1Control = configUI.FindControlForPropertyName(PropertyName.AlphaOp);
            Amount1Control.SetValueDisplayName(AlphaOp.Add, "Add");
            Amount1Control.SetValueDisplayName(AlphaOp.Multiply, "Multiply");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            for (int i = 0; i < 4; i++)
            {
                props.Add(new Int32Property((PropertyName)i, 0, -255, 255));
            }
            props.Add(StaticListChoiceProperty.CreateForEnum<AlphaOp>(PropertyName.AlphaOp, AlphaOp.Add));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(ColorChannels).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            for (int i = 0; i < 4; i++)
            {
                offsets[i] = newToken.GetProperty<Int32Property>((PropertyName)i).Value;
            }
            alphaOp = (AlphaOp)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.AlphaOp).Value;
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
                    ColorBgra color = src[x, y];

                    for (int i = 0; i < 3; i++)
                    {
                        color[CHANNEL_MAP[i]] = ClampToByte(color[CHANNEL_MAP[i]] + offsets[i]);
                    }

                    switch (alphaOp)
                    {
                        case AlphaOp.Add:
                            color.A = ClampToByte(color.A + offsets[3]);
                            break;
                        case AlphaOp.Multiply:
                            float factor = (offsets[3] + 255) / 255.0f;
                            color.A = ClampToByte((int)Math.Round(color.A * factor));
                            break;
                    }

                    dst[x, y] = color;
                }
            }
        }

        private static byte ClampToByte(int x)
        {
            if (x < 0)
            {
                return 0;
            }
            else if (x >= 256)
            {
                return 255;
            }
            else
            {
                return (byte)x;
            }
        }
    }
}
