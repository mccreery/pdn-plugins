using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.UnpackChannel
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class UnpackChannel : PropertyBasedEffect
    {
        public enum PropertyName
        {
            InputChannel,
            OutputChannels,
            Invert
        }

        public enum InputChannel
        {
            Red,
            Green,
            Blue,
            Alpha
        }

        public static int GetBgraChannel(InputChannel inputChannel)
        {
            switch (inputChannel)
            {
                case InputChannel.Red: return 2;
                case InputChannel.Green: return 1;
                case InputChannel.Blue: return 0;
                case InputChannel.Alpha: return 3;
                default: throw new ArgumentException("Invalid input channel");
            }
        }

        public enum OutputChannels
        {
            Grayscale,
            Red,
            Green,
            Blue,
            Alpha
        }

        public static uint GetBgraMask(OutputChannels outputChannels)
        {
            switch (outputChannels)
            {
                case OutputChannels.Grayscale: return (uint)ColorBgra.FromBgra(255, 255, 255, 0);
                case OutputChannels.Red: return (uint)ColorBgra.FromBgra(0, 0, 255, 0);
                case OutputChannels.Green: return (uint)ColorBgra.FromBgra(0, 255, 0, 0);
                case OutputChannels.Blue: return (uint)ColorBgra.FromBgra(255, 0, 0, 0);
                case OutputChannels.Alpha: return (uint)ColorBgra.FromBgra(0, 0, 0, 255);
                default: throw new ArgumentException("Invalid output channel");
            }
        }

        private InputChannel inputChannel;
        private OutputChannels outputChannels;
        private bool invert;

        public UnpackChannel() : base(
            typeof(UnpackChannel).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(UnpackChannel), "icon.png"),
            SubmenuNames.Render,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyName.InputChannel, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.InputChannel, ControlInfoPropertyNames.DisplayName, "Input Channel");

            configUI.SetPropertyControlType(PropertyName.OutputChannels, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.OutputChannels, ControlInfoPropertyNames.DisplayName, "Output Channels");

            configUI.SetPropertyControlType(PropertyName.Invert, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.Invert, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyName.Invert, ControlInfoPropertyNames.Description, "Invert");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum(PropertyName.InputChannel, InputChannel.Red));
            props.Add(StaticListChoiceProperty.CreateForEnum(PropertyName.OutputChannels, OutputChannels.Grayscale));
            props.Add(new BooleanProperty(PropertyName.Invert));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(UnpackChannel).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            inputChannel = (InputChannel)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.InputChannel).Value;
            outputChannels = (OutputChannels)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.OutputChannels).Value;
            invert = newToken.GetProperty<BooleanProperty>(PropertyName.Invert).Value;
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
            int bgraChannel = GetBgraChannel(inputChannel);
            uint bgraMask = GetBgraMask(outputChannels);
            uint alphaMask = ~bgraMask & (uint)ColorBgra.FromBgra(0, 0, 0, 255);
            byte invertMask = invert ? (byte)0xff : (byte)0;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    byte value = (byte)(src[x, y][bgraChannel] ^ invertMask);
                    uint allValue = AllComponents(value);

                    dst[x, y] = (ColorBgra)(allValue & bgraMask | alphaMask);
                }
            }
        }

        private static uint AllComponents(byte component)
        {
            return (uint)(component | component << 8 | component << 16 | component << 24);
        }
    }
}
