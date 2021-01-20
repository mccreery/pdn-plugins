using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.LongShadow
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class LongShadow : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            Color,
            Angle,
            ShadowOnly
        }

        private ColorBgra color;
        private double angle;
        private bool shadowOnly;

        public LongShadow() : base(
            typeof(LongShadow).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(LongShadow), "icon.png"),
            SubmenuNames.Render,
            new EffectOptions() { Flags = EffectFlags.Configurable, RenderingSchedule = EffectRenderingSchedule.None })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Color, ControlInfoPropertyNames.DisplayName, "Color");
            configUI.SetPropertyControlType(PropertyNames.Angle, PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue(PropertyNames.Angle, ControlInfoPropertyNames.DisplayName, "Angle");
            configUI.SetPropertyControlType(PropertyNames.ShadowOnly, PropertyControlType.CheckBox);
            configUI.SetPropertyControlValue(PropertyNames.ShadowOnly, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.ShadowOnly, ControlInfoPropertyNames.Description, "Shadow Only");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Color, (int)(uint)EnvironmentParameters.PrimaryColor));
            props.Add(new DoubleProperty(PropertyNames.Angle, -45, -180, 180));
            props.Add(new BooleanProperty(PropertyNames.ShadowOnly, false));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(LongShadow).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            color = (ColorBgra)(uint)newToken.GetProperty<Int32Property>(PropertyNames.Color).Value;
            angle = newToken.GetProperty<DoubleProperty>(PropertyNames.Angle).Value;
            shadowOnly = newToken.GetProperty<BooleanProperty>(PropertyNames.ShadowOnly).Value;

            double radians = MathUtil.DegreesToRadians(angle);
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            rayDirection = new SizeF(cos, -sin);
        }

        private SizeF rayDirection;

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            // Number of rays is equal to the length of the diagonal
            int diagonal = (int)Math.Ceiling(Math.Sqrt(SrcArgs.Width * SrcArgs.Width + SrcArgs.Height * SrcArgs.Height));
            PointF?[] traces = new PointF?[diagonal];

            PointF center = new PointF(SrcArgs.Width / 2, SrcArgs.Height / 2);

            // Wavefront spans perpendicular to the direction
            SizeF step = new SizeF(-rayDirection.Height, rayDirection.Width);

            for (int i = 0; i < traces.Length; i++)
            {
                int j = i - traces.Length / 2;
                PointF origin = new PointF(
                    center.X + step.Width * j,
                    center.Y + step.Height * j);

                traces[i] = new Ray(origin, rayDirection).Trace(SrcArgs, color => color.A >= 128);
            }

            PdnGraphicsPath graphicsPath = new PdnGraphicsPath();

            int polygonStartIndex = 0;
            while (true)
            {
                PointF[] polygon = GetNextPolygon(traces, ref polygonStartIndex);
                if (polygon == null)
                {
                    break;
                }

                graphicsPath.AddPolygon(polygon);
            }

            DstArgs.Surface.Clear(ColorBgra.Transparent);
            DstArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DstArgs.Graphics.FillPath(new SolidBrush(color), graphicsPath);

            if (!shadowOnly)
            {
                DstArgs.Graphics.DrawImage(SrcArgs.Bitmap, Point.Empty);
            }
        }

        private PointF[] GetNextPolygon(PointF?[] traces, ref int startIndex)
        {
            while (startIndex < traces.Length && traces[startIndex] == null)
            {
                startIndex++;
            }
            if (startIndex == traces.Length)
            {
                return null;
            }

            List<PointF> polygon = new List<PointF>();

            while (startIndex < traces.Length && traces[startIndex] != null)
            {
                polygon.Add(traces[startIndex].Value);
                startIndex++;
            }

            // Add reversed points around the edge of the selection to close the path
            for (int i = polygon.Count - 1; i >= 0; i--)
            {
                Ray ray = new Ray(polygon[i], rayDirection);
                polygon.Add(ray[ray.TraceEdge(EnvironmentParameters.SelectionBounds)]);
            }

            return polygon.ToArray();
        }

        private static ColorBgra MultiplyAlpha(ColorBgra color, byte alpha)
        {
            return color.NewAlpha((byte)Math.Round(color.A / 255.0 * alpha));
        }
    }
}
