using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.SignedDistanceField
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class SignedDistanceField : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            AlphaThreshold,
            Scale,
            Bias
        }

        private byte alphaThreshold;
        private float scale;
        private byte bias;

        public SignedDistanceField() : base(
            typeof(SignedDistanceField).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(SignedDistanceField), "icon.png"),
            SubmenuNames.Render,
            new EffectOptions() { Flags = EffectFlags.Configurable, RenderingSchedule = EffectRenderingSchedule.None })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.AlphaThreshold, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.AlphaThreshold, ControlInfoPropertyNames.DisplayName, "Alpha Threshold");
            configUI.SetPropertyControlType(PropertyNames.Scale, PropertyControlType.Slider);
            configUI.SetPropertyControlType(PropertyNames.Bias, PropertyControlType.Slider);

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.AlphaThreshold, 127, 0, 255));
            props.Add(new DoubleProperty(PropertyNames.Scale, 1, 0, 16));
            props.Add(new Int32Property(PropertyNames.Bias, 127, 0, 255));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(SignedDistanceField).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            alphaThreshold = (byte)newToken.GetProperty<Int32Property>(PropertyNames.AlphaThreshold).Value;
            scale = (float)newToken.GetProperty<DoubleProperty>(PropertyNames.Scale).Value;
            bias = (byte)newToken.GetProperty<Int32Property>(PropertyNames.Bias).Value;
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Debug.Assert(length == 1);
            Rectangle rectangle = renderRects[startIndex];
        }
    }
}
