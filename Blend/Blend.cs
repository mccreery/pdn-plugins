using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;

namespace AssortedPlugins.Blend
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public class Blend : PropertyBasedEffect
    {
        private ColorBgra color;
        private CompositionOp blendMode;

        private bool blendColor;
        private bool blendAlpha;
        private bool interpolateColor;
        private bool underlay;

        public Blend() : base(
                typeof(Blend).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(Blend), "icon.png"),
                null,
                new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        private IList<ILocalizedEnumValue> GetLocalizedBlendModes()
        {
            IEnumLocalizerFactory factory = Services.GetService<IEnumLocalizerFactory>();
            IEnumLocalizer blendModeLocalizer = factory.Create(typeof(LayerBlendMode));

            return blendModeLocalizer.GetLocalizedEnumValues();
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(nameof(color), PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(color), ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType(nameof(blendMode), PropertyControlType.DropDown);
            configUI.SetPropertyControlValue(nameof(blendMode), ControlInfoPropertyNames.DisplayName, "Blend Mode");
            // Localize blend mode names
            PropertyControlInfo blendModeControl = configUI.FindControlForPropertyName(nameof(blendMode));
            foreach (ILocalizedEnumValue blendOption in GetLocalizedBlendModes())
            {
                blendModeControl.SetValueDisplayName(blendOption.EnumValue, blendOption.LocalizedName);
            }

            configUI.SetPropertyControlType(nameof(blendColor), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(blendColor), ControlInfoPropertyNames.DisplayName, "Components");
            configUI.SetPropertyControlValue(nameof(blendColor), ControlInfoPropertyNames.Description, "Color");

            configUI.SetPropertyControlType(nameof(blendAlpha), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(blendAlpha), ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(nameof(blendAlpha), ControlInfoPropertyNames.Description, "Alpha");

            configUI.SetPropertyControlType(nameof(interpolateColor), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(interpolateColor), ControlInfoPropertyNames.DisplayName, "Options");
            configUI.SetPropertyControlValue(nameof(interpolateColor), ControlInfoPropertyNames.Description, "Interpolate color");

            configUI.SetPropertyControlType(nameof(underlay), PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(nameof(underlay), ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(nameof(underlay), ControlInfoPropertyNames.Description, "Underlay");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(nameof(color), (int)(uint)EnvironmentParameters.PrimaryColor));
            props.Add(StaticListChoiceProperty.CreateForEnum(nameof(blendMode), LayerBlendMode.Multiply));

            props.Add(new BooleanProperty(nameof(blendColor)));
            props.Add(new BooleanProperty(nameof(blendAlpha)));
            props.Add(new BooleanProperty(nameof(interpolateColor)));
            props.Add(new BooleanProperty(nameof(underlay)));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(Blend).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            color = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(nameof(color)).Value;
            blendMode = LayerBlendModeUtil.CreateCompositionOp(
                (LayerBlendMode)newToken.GetProperty<StaticListChoiceProperty>(nameof(blendMode)).Value);

            blendColor = newToken.GetProperty<BooleanProperty>(nameof(blendColor)).Value;
            blendAlpha= newToken.GetProperty<BooleanProperty>(nameof(blendAlpha)).Value;
            interpolateColor = newToken.GetProperty<BooleanProperty>(nameof(interpolateColor)).Value;
            underlay = newToken.GetProperty<BooleanProperty>(nameof(underlay)).Value;
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
            BlendFunc blendFunc = this.blendMode.GetBlendFunc();
            ColorBgra colorOpaque = color.NewAlpha(255);

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra srcColor = src[x, y];
                    ColorBgra dstColor = srcColor;

                    if (blendColor)
                    {
                        byte tempAlpha = dstColor.A;
                        dstColor.A = 255;
                        dstColor = blendMode.Apply(dstColor, colorOpaque);
                        dstColor.A = tempAlpha;
                    }
                    if (blendAlpha)
                    {
                        dstColor.A = blendFunc(dstColor.A, color.A);
                    }
                    if (interpolateColor)
                    {
                        byte a = dstColor.A;
                        dstColor = ColorBgra.Lerp(srcColor, dstColor, ByteUtil.ToScalingFloat(color.A));
                        dstColor.A = a;
                    }
                    if (underlay)
                    {
                        dstColor = UserBlendOps.NormalBlendOp.ApplyStatic(color, dstColor);
                    }

                    dst[x, y] = dstColor;
                }
            }
        }
    }
}
