using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.GrowAndShrink
{
    [PluginSupportInfo(typeof(AssemblyPluginInfo))]
    public partial class GrowAndShrinkEffect : PropertyBasedEffect
    {
        private Int32 exampleProperty;

        public GrowAndShrinkEffect() : base(
                typeof(GrowAndShrinkEffect).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(GrowAndShrinkEffect), "icon.png"),
                SubmenuNames.Render,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(nameof(exampleProperty),
                PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(exampleProperty),
                ControlInfoPropertyNames.DisplayName, "Example Property");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(new Int32Property(nameof(exampleProperty),
                ColorBgra.ToOpaqueInt32(primaryColor), 0x000000, 0xffffff));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(GrowAndShrinkEffect).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            exampleProperty = newToken.GetProperty<Int32Property>(nameof(exampleProperty)).Value;
        }
    }
}
