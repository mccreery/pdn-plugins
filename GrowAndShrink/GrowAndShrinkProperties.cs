using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private int radius;
        private ColorBgra color;
        private SmoothingMode smoothingMode;

        public GrowAndShrinkEffect() : base(
                typeof(GrowAndShrinkEffect).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(GrowAndShrinkEffect), "icon.png"),
                SubmenuNames.Distort,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(nameof(radius), PropertyControlType.Slider);
            configUI.SetPropertyControlValue(nameof(radius), ControlInfoPropertyNames.DisplayName, "Radius");

            configUI.SetPropertyControlType(nameof(color),
                PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(color),
                ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType(nameof(smoothingMode), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(nameof(smoothingMode), ControlInfoPropertyNames.Description, "Antialiasing");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(new Int32Property(nameof(radius), 0, -50, 50));
            props.Add(new Int32Property(nameof(color),
                ColorBgra.ToOpaqueInt32(primaryColor.NewAlpha(255)),
                0x000000, 0xffffff));

            props.Add(new BooleanProperty(nameof(smoothingMode), false));

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

            radius = newToken.GetProperty<Int32Property>(nameof(radius)).Value;
            // TODO
            radius = Math.Abs(radius);

            color = ColorBgra.FromOpaqueInt32(
                newToken.GetProperty<Int32Property>(nameof(color)).Value);

            smoothingMode = newToken.GetProperty<BooleanProperty>(nameof(smoothingMode)).Value
                ? SmoothingMode.AntiAlias : SmoothingMode.Default;
        }
    }
}
