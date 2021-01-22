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
            Type,
            Color
        }

        public enum Type
        {
            Cutout,
            Stencil
        }

        private Type type;
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

            configUI.SetPropertyControlType(PropertyNames.Type, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyNames.Type, ControlInfoPropertyNames.DisplayName, "Type");
            PropertyControlInfo typeControlInfo = configUI.FindControlForPropertyName(PropertyNames.Type);
            typeControlInfo.SetValueDisplayName(Type.Cutout, "Cutout (normal alpha)");
            typeControlInfo.SetValueDisplayName(Type.Stencil, "Stencil (inverted alpha)");

            configUI.SetPropertyControlType(PropertyNames.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Color, ControlInfoPropertyNames.DisplayName, "Color");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum<Type>(PropertyNames.Type, Type.Cutout));
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

            type = (Type)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Type).Value;
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
                    byte srcAlpha = src[x, y].A;

                    if (type == Type.Stencil)
                    {
                        srcAlpha = (byte)(255 - srcAlpha);
                    }

                    dst[x, y] = stencilColor.NewAlpha(ByteUtil.FastScale(stencilColor.A, srcAlpha));
                }
            }
        }
    }
}
