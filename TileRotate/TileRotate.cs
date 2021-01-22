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
        public enum PropertyName
        {
            Mode
        }

        public enum Mode
        {
            SwapLeftRight,
            SwapTopBottom,
            SwapQuadrants
        }

        private Mode mode;

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

            configUI.SetPropertyControlType(PropertyName.Mode, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyName.Mode, ControlInfoPropertyNames.DisplayName, "");

            PropertyControlInfo Amount1Control = configUI.FindControlForPropertyName(PropertyName.Mode);
            Amount1Control.SetValueDisplayName(Mode.SwapLeftRight, "Swap left and right halves");
            Amount1Control.SetValueDisplayName(Mode.SwapTopBottom, "Swap top and bottom halves");
            Amount1Control.SetValueDisplayName(Mode.SwapQuadrants, "Swap diagonal quadrants");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(StaticListChoiceProperty.CreateForEnum<Mode>(PropertyName.Mode, Mode.SwapQuadrants));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(TileRotate).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

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

    public enum OffsetMode
    {
        Relative,
        Absolute
    }
}
