using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.Expression
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class Expression : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            Program,
            LicenseLink
        }

        private string program;

        public Expression() : base(
            typeof(Expression).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(Expression), "icon.png"),
            "Advanced",
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Program, PropertyControlType.TextBox);
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.Multiline, true);
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.Description,
                "Write 1-4 expressions separated by newlines. Each line corresponds to an output channel depending on the number of lines:\r\n" +
                "\t\u2022 1 \u2192 RGB\r\n\t\u2022 2 \u2192 RGB, A\r\n\t\u2022 3 \u2192 R, G, B\r\n\t\u2022 4 \u2192 R, G, B, A\r\n\r\n" +
                "The variables r, g, b, a and x (the current channel) are provided.");

            configUI.SetPropertyControlType(PropertyNames.LicenseLink, PropertyControlType.LinkLabel);
            configUI.SetPropertyControlValue(PropertyNames.LicenseLink, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.LicenseLink, ControlInfoPropertyNames.Description, "Parser license");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new StringProperty(PropertyNames.Program, "x"));
            props.Add(new UriProperty(PropertyNames.LicenseLink, new Uri("https://not.real/")));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(Expression).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            program = newToken.GetProperty<StringProperty>(PropertyNames.Program).Value;
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
