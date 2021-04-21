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
        private int exampleProperty;

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

            configUI.SetPropertyControlType(nameof(exampleProperty), PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(exampleProperty), ControlInfoPropertyNames.DisplayName, "Example Property");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(nameof(exampleProperty), (int)(uint)EnvironmentParameters.PrimaryColor));

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
            exampleProperty = newToken.GetProperty<Int32Property>(nameof(exampleProperty)).Value;
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
        }
    }
}
