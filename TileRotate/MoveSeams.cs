using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.MoveSeams
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class MoveSeams : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            OffsetMode,
            AbsoluteXOffset,
            AbsoluteYOffset,
            RelativeOffset
        }

        public enum OffsetMode
        {
            Absolute,
            Relative
        }

        private Size offset;

        public MoveSeams() : base(
            typeof(MoveSeams).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(MoveSeams), "icon.png"),
            SubmenuNames.Distort,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.OffsetMode, ControlInfoPropertyNames.DisplayName, "Offset Mode");
            configUI.SetPropertyControlType(PropertyNames.OffsetMode, PropertyControlType.RadioButton);

            PropertyControlInfo modeControl = configUI.FindControlForPropertyName(PropertyNames.OffsetMode);
            modeControl.SetValueDisplayName(OffsetMode.Absolute, "Absolute");
            modeControl.SetValueDisplayName(OffsetMode.Relative, "Relative");

            configUI.SetPropertyControlType(PropertyNames.AbsoluteXOffset, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.AbsoluteXOffset, ControlInfoPropertyNames.DisplayName, "Absolute X Offset");
            configUI.SetPropertyControlType(PropertyNames.AbsoluteYOffset, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.AbsoluteYOffset, ControlInfoPropertyNames.DisplayName, "Absolute Y Offset");

            configUI.SetPropertyControlType(PropertyNames.RelativeOffset, PropertyControlType.PanAndSlider);
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.DisplayName, "Relative Offset");

            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.SliderSmallChangeX, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.SliderLargeChangeX, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.UpDownIncrementX, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.SliderSmallChangeY, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.SliderLargeChangeY, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.UpDownIncrementY, 0.01);

            ImageResource underlay = ImageResource.FromImage(EnvironmentParameters.SourceSurface.CreateAliasedBitmap(EnvironmentParameters.SelectionBounds));
            configUI.SetPropertyControlValue(PropertyNames.RelativeOffset, ControlInfoPropertyNames.StaticImageUnderlay, GetTiledUnderlay());

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum<OffsetMode>(PropertyNames.OffsetMode, OffsetMode.Absolute, false));

            Rectangle bounds = EnvironmentParameters.SelectionBounds;
            props.Add(new Int32Property(PropertyNames.AbsoluteXOffset, (int)Math.Round(bounds.Width / 2.0), -bounds.Width, bounds.Width));
            props.Add(new Int32Property(PropertyNames.AbsoluteYOffset, (int)Math.Round(bounds.Height / 2.0), -bounds.Height, bounds.Height));

            props.Add(new DoubleVectorProperty(PropertyNames.RelativeOffset, Pair.Create(0.5, 0.5), Pair.Create(-1.0, -1.0), Pair.Create(1.0, 1.0)));

            List<PropertyCollectionRule> rules = new List<PropertyCollectionRule>();

            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(PropertyNames.RelativeOffset, PropertyNames.OffsetMode, OffsetMode.Relative, true));
            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(PropertyNames.AbsoluteXOffset, PropertyNames.OffsetMode, OffsetMode.Absolute, true));
            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(PropertyNames.AbsoluteYOffset, PropertyNames.OffsetMode, OffsetMode.Absolute, true));

            return new PropertyCollection(props, rules);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(MoveSeams).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            Rectangle bounds = EnvironmentParameters.SelectionBounds;

            if ((OffsetMode)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.OffsetMode).Value == OffsetMode.Relative)
            {
                Pair<double, double> offsetD = newToken.GetProperty<DoubleVectorProperty>(PropertyNames.RelativeOffset).Value;
                offset = new Size(
                    (int)Math.Round(bounds.Width * offsetD.First),
                    (int)Math.Round(bounds.Height * offsetD.Second));
            }
            else
            {
                offset = new Size(
                    newToken.GetProperty<Int32Property>(PropertyNames.AbsoluteXOffset).Value,
                    newToken.GetProperty<Int32Property>(PropertyNames.AbsoluteYOffset).Value);
            }

            mode = (Mode)newToken.GetProperty<StaticListChoiceProperty>(PropertyName.Mode).Value;
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Rectangle bounds = EnvironmentParameters.SelectionBounds;

            int endIndex = startIndex + length;
            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            Rectangle selection = EnvironmentParameters.SelectionBounds;

            Point topLeft = selection.Location;
            Point topRight = new Point(selection.X + selection.Width / 2, selection.Y);
            Point bottomLeft = new Point(selection.X, selection.Y + selection.Height / 2);
            Point bottomRight = new Point(selection.X + selection.Width / 2, selection.Y + selection.Height / 2);

            Size chunkSize;
            switch (mode)
            {
                case Mode.SwapLeftRight:
                    chunkSize = new Size(selection.Width / 2, selection.Height);

                    CopySurfacePart(dst, rect, topLeft, src, new Rectangle(topRight, chunkSize));
                    CopySurfacePart(dst, rect, topRight, src, new Rectangle(topLeft, chunkSize));
                    break;
                case Mode.SwapTopBottom:
                    chunkSize = new Size(selection.Width, selection.Height / 2);

                    CopySurfacePart(dst, rect, topLeft, src, new Rectangle(bottomLeft, chunkSize));
                    CopySurfacePart(dst, rect, bottomLeft, src, new Rectangle(topLeft, chunkSize));
                    break;
                case Mode.SwapQuadrants:
                    chunkSize = new Size(selection.Width / 2, selection.Height / 2);

                    CopySurfacePart(dst, rect, topLeft, src, new Rectangle(bottomRight, chunkSize));
                    CopySurfacePart(dst, rect, topRight, src, new Rectangle(bottomLeft, chunkSize));
                    CopySurfacePart(dst, rect, bottomLeft, src, new Rectangle(topRight, chunkSize));
                    CopySurfacePart(dst, rect, bottomRight, src, new Rectangle(topLeft, chunkSize));
                    break;
            }
        }

        private static void CopySurfacePart(Surface dst, Rectangle dstBounds, Point dstOffset, Surface src, Rectangle srcRect)
        {
            Rectangle dstRect = new Rectangle(dstOffset, srcRect.Size);
            Rectangle dstRectClamped = dstRect;
            dstRectClamped.Intersect(dstBounds);

            dstOffset = dstRectClamped.Location;
            srcRect = new Rectangle(dstRectClamped.Location - (Size)dstRect.Location + (Size)srcRect.Location, dstRectClamped.Size);

            dst.CopySurface(src, dstOffset, srcRect);
        }
    }
}
