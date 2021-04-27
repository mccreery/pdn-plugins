using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.SignedDistanceField
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class SignedDistanceField : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            AlphaThreshold,
            Scale,
            Bias
        }

        private bool fieldsDirty = true;
        private Size[,] distanceField;
        private Size[,] invertedField;
        private Rectangle rectangle;

        private byte alphaThreshold;
        private byte AlphaThreshold
        {
            get => alphaThreshold;
            set
            {
                if (value != alphaThreshold)
                {
                    alphaThreshold = value;
                    fieldsDirty = true;
                }
            }
        }

        private float scale;
        private byte bias;

        public SignedDistanceField() : base(
            typeof(SignedDistanceField).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(SignedDistanceField), "icon.png"),
            "Texture",
            new EffectOptions() { Flags = EffectFlags.Configurable, RenderingSchedule = EffectRenderingSchedule.None })
        {
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.AlphaThreshold, PropertyControlType.Slider);
            configUI.SetPropertyControlValue(PropertyNames.AlphaThreshold, ControlInfoPropertyNames.DisplayName, "Alpha Threshold");
            configUI.SetPropertyControlType(PropertyNames.Scale, PropertyControlType.Slider);
            configUI.SetPropertyControlType(PropertyNames.Bias, PropertyControlType.Slider);

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.AlphaThreshold, 128, 0, 255));
            props.Add(new DoubleProperty(PropertyNames.Scale, 1, 0, 16));
            props.Add(new Int32Property(PropertyNames.Bias, 128, 0, 255));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(SignedDistanceField).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            AlphaThreshold = (byte)newToken.GetProperty<Int32Property>(PropertyNames.AlphaThreshold).Value;
            scale = (float)newToken.GetProperty<DoubleProperty>(PropertyNames.Scale).Value;
            bias = (byte)newToken.GetProperty<Int32Property>(PropertyNames.Bias).Value;
        }

        private static readonly Size Right = new Size(1, 0);
        private static readonly Size RightDown = new Size(1, 1);
        private static readonly Size Down = new Size(0, 1);
        private static readonly Size LeftDown = new Size(-1, 1);
        private static readonly Size Left = new Size(-1, 0);
        private static readonly Size LeftUp = new Size(-1, -1);
        private static readonly Size Up = new Size(0, -1);
        private static readonly Size RightUp = new Size(1, -1);

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            rectangle = renderRects[startIndex];
            for(int i = 1; i < length; i++)
            {
                rectangle = Rectangle.Union(rectangle, renderRects[startIndex + i]);
            }

            if (fieldsDirty)
            {
                GenerateFields();
            }

            for (int y = 0; y < rectangle.Height; y++)
            {
                if (IsCancelRequested) { break; }
                for (int x = 0; x < rectangle.Width; x++)
                {
                    float positiveInfluence = distanceField[y, x].Magnitude();
                    float negativeInfluence = invertedField[y, x].Magnitude();
                    // 1 pixel gap to remove jump from -1 to 1 at border
                    negativeInfluence = Math.Max(0, negativeInfluence - 1);

                    float signedDistance = positiveInfluence - negativeInfluence;
                    byte brightness = (byte)Math.Max(0, Math.Min(255, Math.Round(signedDistance * scale + bias)));

                    DstArgs.Surface[rectangle.X + x, rectangle.Y + y] = ColorBgra.FromBgr(brightness, brightness, brightness);
                }
            }

            if (!IsCancelRequested)
            {
                fieldsDirty = false;
            }
        }

        private void GenerateFields()
        {
            distanceField = new Size[rectangle.Height, rectangle.Width];
            invertedField = new Size[rectangle.Height, rectangle.Width];

            // Initialize distance to infinity outside the shape and 0 inside
            // Inverted field is initialized with the opposite
            for (int y = 0; y < rectangle.Height; y++)
            {
                if (IsCancelRequested) { break; }

                for (int x = 0; x < rectangle.Width; x++)
                {
                    if (SrcArgs.Surface[rectangle.X + x, rectangle.Y + y].A >= AlphaThreshold)
                    {
                        distanceField[y, x] = Size.Empty;
                        invertedField[y, x] = rectangle.Size;
                    }
                    else
                    {
                        distanceField[y, x] = rectangle.Size;
                        invertedField[y, x] = Size.Empty;
                    }
                }
            }

            Parallel.Invoke(
                () => ScanDown(distanceField),
                () => ScanUp(distanceField),
                () => ScanDown(invertedField),
                () => ScanUp(invertedField));
        }

        private void ScanDown(Size[,] distanceField)
        {
            for (int y = 1; y < rectangle.Height; y++)
            {
                if (IsCancelRequested) { break; }
                for (int x = 0; x < rectangle.Width; x++)
                {
                    Size currentDistance = distanceField[y, x];

                    if (x > 0) currentDistance = MinMagnitude(currentDistance, distanceField[y, x - 1] + Left);
                    currentDistance = MinMagnitude(currentDistance, distanceField[y - 1, x] + Up);

                    distanceField[y, x] = currentDistance;
                }

                for (int x = rectangle.Width - 2; x >= 0; x--)
                {
                    distanceField[y, x] = MinMagnitude(distanceField[y, x], distanceField[y, x + 1] + Right);
                }
            }
        }

        private void ScanUp(Size[,] distanceField)
        {
            for (int y = rectangle.Height - 2; y >= 0; y--)
            {
                if (IsCancelRequested) { break; }
                for (int x = 0; x < rectangle.Width; x++)
                {
                    Size currentDistance = distanceField[y, x];

                    if (x > 0) currentDistance = MinMagnitude(currentDistance, distanceField[y, x - 1] + Left);
                    currentDistance = MinMagnitude(currentDistance, distanceField[y + 1, x] + Down);

                    distanceField[y, x] = currentDistance;
                }

                for (int x = rectangle.Width - 2; x >= 0; x--)
                {
                    distanceField[y, x] = MinMagnitude(distanceField[y, x], distanceField[y, x + 1] + Right);
                }
            }
        }

        private static Size MinMagnitude(Size a, Size b)
        {
            return a.MagnitudeSquared() < b.MagnitudeSquared() ? a : b;
        }
    }
}
