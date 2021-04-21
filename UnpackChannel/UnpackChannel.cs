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
            Channel,
            MaskType,
            Invert
        }

        public enum Channel
        {
            Red,
            Green,
            Blue,
            Alpha
        }

        private static readonly int[] rgbaToBgra = { 2, 1, 0, 3 };

        public enum MaskType
        {
            Grayscale,
            Opacity
        }

        private Channel channel;
        private MaskType maskType;
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

            configUI.SetPropertyControlType(PropertyName.Channel, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.Channel, ControlInfoPropertyNames.DisplayName, "Channel");

            configUI.SetPropertyControlType(PropertyName.MaskType, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.MaskType, ControlInfoPropertyNames.DisplayName, "Mask Type");

            configUI.SetPropertyControlType(PropertyName.Invert, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyName.Invert, ControlInfoPropertyNames.DisplayName, "Invert");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum(PropertyName.Channel, Channel.Red));
            props.Add(StaticListChoiceProperty.CreateForEnum(PropertyName.MaskType, MaskType.Grayscale));
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

            channel = (Channel)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.Channel).Value;
            maskType = (MaskType)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.MaskType).Value;
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
            int bgraChannel = rgbaToBgra[(int)channel];
            byte invertMask = invert ? (byte)255 : (byte)0;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    byte channelValue = (byte)(src[x, y][bgraChannel] ^ invertMask);

                    if (maskType == MaskType.Grayscale)
                    {
                        dst[x, y] = ColorBgra.FromBgr(channelValue, channelValue, channelValue);
                    }
                    else
                    {
                        dst[x, y] = ColorBgra.Black.NewAlpha(channelValue);
                    }
                }
            }
        }
    }
}
