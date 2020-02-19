using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.TileRotate
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class TileRotate : PropertyBasedEffect
    {
        private Size offset;

        public TileRotate() : base(
            typeof(TileRotate).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(TileRotate), "icon.png"),
            SubmenuNames.Distort,
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue("mode", ControlInfoPropertyNames.DisplayName, "Mode");
            configUI.SetPropertyControlType("mode", PropertyControlType.RadioButton);

            PropertyControlInfo modeControl = configUI.FindControlForPropertyName("mode");
            modeControl.SetValueDisplayName(nameof(OffsetMode.Relative), "Relative");
            modeControl.SetValueDisplayName(nameof(OffsetMode.Absolute), "Absolute");

            configUI.SetPropertyControlType("pan", PropertyControlType.PanAndSlider);
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.DisplayName, "Relative Offset");

            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.SliderSmallChangeX, 0.05);
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.SliderLargeChangeX, 0.25);
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.UpDownIncrementX, 0.05);

            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.SliderSmallChangeY, 0.05);
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.SliderLargeChangeY, 0.25);
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.UpDownIncrementY, 0.05);

            ImageResource underlay = ImageResource.FromImage(EnvironmentParameters.SourceSurface.CreateAliasedBitmap(EnvironmentParameters.SelectionBounds));
            configUI.SetPropertyControlValue("pan", ControlInfoPropertyNames.StaticImageUnderlay, GetTiledUnderlay());

            configUI.SetPropertyControlType("offsetX", PropertyControlType.Slider);
            configUI.SetPropertyControlValue("offsetX", ControlInfoPropertyNames.DisplayName, "Absolute X Offset");
            configUI.SetPropertyControlType("offsetY", PropertyControlType.Slider);
            configUI.SetPropertyControlValue("offsetY", ControlInfoPropertyNames.DisplayName, "Absolute Y Offset");

            return configUI;
        }

        private ImageResource GetTiledUnderlay()
        {
            Image selection = EnvironmentParameters.SourceSurface.CreateAliasedBitmap(
                EnvironmentParameters.SelectionBounds);

            Image underlay = new Bitmap(selection.Size.Width * 2, selection.Size.Height * 2);
            Graphics g = Graphics.FromImage(underlay);

            g.DrawImage(selection, 0, 0);
            g.DrawImage(selection, selection.Width, 0);
            g.DrawImage(selection, 0, selection.Height);
            g.DrawImage(selection, selection.Width, selection.Height);

            return ImageResource.FromImage(underlay);
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum<OffsetMode>("mode", OffsetMode.Relative, false));
            props.Add(new DoubleVectorProperty("pan", Pair.Create(0.0, 0.0), Pair.Create(-1.0, -1.0), Pair.Create(1.0, 1.0)));
            props.Add(new Int32Property("offsetX", 0, -256, 256));
            props.Add(new Int32Property("offsetY", 0, -256, 256));

            List<PropertyCollectionRule> rules = new List<PropertyCollectionRule>();

            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>("pan", "mode", OffsetMode.Relative, true));
            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>("offsetX", "mode", OffsetMode.Absolute, true));
            rules.Add(new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>("offsetY", "mode", OffsetMode.Absolute, true));

            return new PropertyCollection(props, rules);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(TileRotate).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            Rectangle bounds = EnvironmentParameters.SelectionBounds;

            if ((OffsetMode)newToken.GetProperty<StaticListChoiceProperty>("mode").Value == OffsetMode.Relative)
            {
                Pair<double, double> offsetD = newToken.GetProperty<DoubleVectorProperty>("pan").Value;
                offset = new Size(
                    (int)Math.Round(bounds.Width * offsetD.First),
                    (int)Math.Round(bounds.Height * offsetD.Second));
            }
            else
            {
                offset = new Size(
                    newToken.GetProperty<Int32Property>("offsetX").Value,
                    newToken.GetProperty<Int32Property>("offsetY").Value);
            }

            offset.Width = FloorMod(offset.Width, bounds.Width);
            offset.Height = FloorMod(offset.Height, bounds.Height);
        }

        private readonly Pair<Rectangle, Point>[] srcDst = new Pair<Rectangle, Point>[4];

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Rectangle bounds = EnvironmentParameters.SelectionBounds;

            srcDst[0] = Pair.Create(new Rectangle(bounds.Location, bounds.Size - offset), bounds.Location + offset);
            srcDst[1] = Pair.Create( new Rectangle(bounds.Location + bounds.Size - offset, offset), bounds.Location);

            srcDst[2] = Pair.Create(
                new Rectangle(bounds.Location + new Size(0, bounds.Height - offset.Height), new Size(bounds.Width - offset.Width, offset.Height)),
                bounds.Location + new Size(offset.Width, 0));
            srcDst[3] = Pair.Create(
                new Rectangle(bounds.Location + new Size(bounds.Width - offset.Width, 0), new Size(offset.Width, bounds.Height - offset.Height)),
                bounds.Location + new Size(0, offset.Height));

            int endIndex = startIndex + length;
            for (int i = startIndex; i < endIndex; i++)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            foreach (Pair<Rectangle, Point> p in srcDst)
            {
                CopySurfacePart(dst, rect, p.Second, src, p.First);
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

        private static int FloorMod(int a, int b)
        {
            return ((a % b) + b) % b;
        }
    }

    public enum OffsetMode
    {
        Relative,
        Absolute
    }
}
