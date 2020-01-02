using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace AssortedPlugins.Blend
{
    [PluginSupportInfo(typeof(AssemblyPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public partial class BlendEffect : PropertyBasedEffect
    {
        private ColorBgra color;
        private CompositionOp blendMode;
        private Flags flags;

        [Flags]
        private enum Flags
        {
            Color = 1,
            Alpha = 2,
            InterpolateColor = 4,
            Underlay = 8
        }
        private static readonly Flags[] flagValues = (Flags[])Enum.GetValues(typeof(Flags));

        public BlendEffect() : base(
                typeof(BlendEffect).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
                new Bitmap(typeof(BlendEffect), "icon.png"),
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

            foreach (Flags flag in flagValues)
            {
                configUI.SetPropertyControlType(flag, PropertyControlType.CheckBox);
                configUI.SetPropertyControlValue(flag, ControlInfoPropertyNames.DisplayName, "");
            }
            configUI.SetPropertyControlValue(Flags.Color, ControlInfoPropertyNames.DisplayName, "Components");
            configUI.SetPropertyControlValue(Flags.Color, ControlInfoPropertyNames.Description, "Color");
            configUI.SetPropertyControlValue(Flags.Alpha, ControlInfoPropertyNames.Description, "Alpha");
            configUI.SetPropertyControlValue(Flags.InterpolateColor, ControlInfoPropertyNames.DisplayName, "Options");
            configUI.SetPropertyControlValue(Flags.InterpolateColor, ControlInfoPropertyNames.Description, "Interpolate color");
            configUI.SetPropertyControlValue(Flags.Underlay, ControlInfoPropertyNames.Description, "Underlay");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(nameof(color), (int)(uint)EnvironmentParameters.PrimaryColor));
            props.Add(StaticListChoiceProperty.CreateForEnum(nameof(blendMode), LayerBlendMode.Multiply));

            foreach (Flags flag in flagValues)
            {
                props.Add(new BooleanProperty(flag));
            }

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);

            props[ControlInfoPropertyNames.WindowTitle].Value =
                typeof(BlendEffect).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            color = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(nameof(color)).Value;
            blendMode = LayerBlendModeUtil.CreateCompositionOp(
                (LayerBlendMode)newToken.GetProperty<StaticListChoiceProperty>(nameof(blendMode)).Value);

            flags = 0;
            foreach (Flags flag in flagValues)
            {
                if (newToken.GetProperty<BooleanProperty>(flag).Value)
                {
                    flags |= flag;
                }
            }
        }
    }
}
