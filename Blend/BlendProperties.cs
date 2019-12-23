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
    [PluginSupportInfo(typeof(AssemblyPluginInfo))]
    [EffectCategory(EffectCategory.Adjustment)]
    public partial class BlendEffect : PropertyBasedEffect
    {
        private CompositionOp blendMode;
        private ColorBgra color;

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

            configUI.SetPropertyControlType(nameof(blendMode), PropertyControlType.DropDown);
            configUI.SetPropertyControlValue(nameof(blendMode), ControlInfoPropertyNames.DisplayName, "Blend Mode");

            // Localize blend mode names
            PropertyControlInfo blendModeControl = configUI.FindControlForPropertyName(nameof(blendMode));
            foreach (ILocalizedEnumValue blendOption in GetLocalizedBlendModes())
            {
                blendModeControl.SetValueDisplayName(blendOption.EnumValue, blendOption.LocalizedName);
            }

            configUI.SetPropertyControlType(nameof(color), PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(nameof(color), ControlInfoPropertyNames.DisplayName, "Color");

            configUI.SetPropertyControlType("alpha", PropertyControlType.Slider);
            configUI.SetPropertyControlValue("alpha", ControlInfoPropertyNames.DisplayName, "Alpha");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();
            ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;

            props.Add(StaticListChoiceProperty.CreateForEnum(nameof(blendMode), LayerBlendMode.Multiply));

            props.Add(new Int32Property(nameof(color),
                ColorBgra.ToOpaqueInt32(primaryColor), 0x000000, 0xffffff));

            props.Add(new Int32Property("alpha", 255, 0, 255));

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

            blendMode = LayerBlendModeUtil.CreateCompositionOp(
                (LayerBlendMode)newToken.GetProperty<StaticListChoiceProperty>(nameof(blendMode)).Value);

            byte alpha = (byte)newToken.GetProperty<Int32Property>(nameof(alpha)).Value;
            color = ColorBgra.FromOpaqueInt32(newToken.GetProperty<Int32Property>(nameof(color)).Value);
            color = color.NewAlpha(alpha);
        }
    }
}
