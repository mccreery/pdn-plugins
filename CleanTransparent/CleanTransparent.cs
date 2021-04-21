using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.CleanTransparent
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class CleanTransparent : PropertyBasedEffect
    {
        public enum PropertyName
        {
            TransparentFillColor
        }

        private ColorBgra transparentFillColor;

        public CleanTransparent() : base(
            typeof(CleanTransparent).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(CleanTransparent), "icon.png"),
            SubmenuNames.Render,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyName.TransparentFillColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyName.TransparentFillColor, ControlInfoPropertyNames.DisplayName, "Transparent Fill Color");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyName.TransparentFillColor, ColorBgra.ToOpaqueInt32(ColorBgra.Black), 0, 0xffffff));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(CleanTransparent).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            transparentFillColor = ColorBgra.FromOpaqueInt32(
                newToken.GetProperty<Int32Property>(PropertyName.TransparentFillColor).Value).NewAlpha(0);
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

                    if (srcColor.A == 0)
                    {
                        dst[x, y] = transparentFillColor;
                    }
                    else
                    {
                        dst[x, y] = srcColor;
                    }
                }
            }
        }
    }
}
