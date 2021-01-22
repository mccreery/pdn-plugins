using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.Stencil
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class Stencil : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            Color
        }

        private ColorBgra stencilColor;

        public Stencil() : base(
            typeof(Stencil).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(Stencil), "icon.png"),
            "Object",
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Color, ControlInfoPropertyNames.DisplayName, "Color");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.Color, (int)(uint)EnvironmentParameters.PrimaryColor));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(Stencil).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            stencilColor = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(PropertyNames.Color).Value;
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
                    dst[x, y] = stencilColor.NewAlpha(ByteUtil.FastScale(stencilColor.A, (byte)(255 - src[x, y].A)));
                }
            }
        }
    }
}
