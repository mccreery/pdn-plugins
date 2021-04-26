using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
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
        private VectorField distanceField;
        private VectorField invertedField;
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
                fieldsDirty = false;
            }

            Point position = default;
            for (position.Y = rectangle.Top; position.Y < rectangle.Bottom; position.Y++)
            {
                if (IsCancelRequested) { break; }

                for (position.X = rectangle.Left; position.X < rectangle.Right; position.X++)
                {
                    Point fieldPosition = position - (Size)rectangle.Location;

                    float positiveInfluence = distanceField[fieldPosition].Magnitude();
                    float negativeInfluence = invertedField[fieldPosition].Magnitude();
                    // 1 pixel gap to remove jump from -1 to 1 at border
                    negativeInfluence = Math.Max(0, negativeInfluence - 1);

                    float signedDistance = positiveInfluence - negativeInfluence;
                    byte brightness = (byte)Math.Max(0, Math.Min(255, Math.Round(signedDistance * scale + bias)));

                    DstArgs.Surface[position] = ColorBgra.FromBgr(brightness, brightness, brightness);
                }
            }
        }

        private void GenerateFields()
        {
            distanceField = new VectorField(rectangle.Size);
            invertedField = new VectorField(rectangle.Size);

            // Initialize distance to infinity outside the shape and 0 inside
            // Inverted field is initialized with the opposite
            Point position = default;

            for (position.Y = rectangle.Top; position.Y < rectangle.Bottom; position.Y++)
            {
                if (IsCancelRequested) { break; }

                for (position.X = rectangle.Left; position.X < rectangle.Right; position.X++)
                {
                    Point fieldPosition = position - (Size)rectangle.Location;

                    if (SrcArgs.Surface[position].A >= AlphaThreshold)
                    {
                        distanceField[fieldPosition] = SizeF.Empty;
                        invertedField[fieldPosition] = new SizeF(float.PositiveInfinity, float.PositiveInfinity);
                    }
                    else
                    {
                        distanceField[fieldPosition] = new SizeF(float.PositiveInfinity, float.PositiveInfinity);
                        invertedField[fieldPosition] = SizeF.Empty;
                    }
                }
            }

            FastSweep(distanceField);
            FastSweep(invertedField);
        }

        private void FastSweep(VectorField distanceField)
        {
            Point position = default;

            for (position.Y = 0; position.Y < distanceField.Height; position.Y++)
            {
                if (IsCancelRequested) { break; }

                for (position.X = 0; position.X < distanceField.Width; position.X++)
                {
                    SizeF currentDistance = distanceField[position];

                    foreach (Size offset in new[] { Left, Up, LeftUp, RightUp })
                    {
                        currentDistance = MinMagnitude(currentDistance, distanceField[position + offset] + offset);
                    }
                    distanceField[position] = currentDistance;
                }

                for (position.X = distanceField.Width - 1; position.X >= 0; position.X--)
                {
                    distanceField[position] = MinMagnitude(distanceField[position], distanceField[position + Right] + Right);
                }
            }

            for (position.Y = distanceField.Height - 1; position.Y >= 0; position.Y--)
            {
                if (IsCancelRequested) { break; }

                for (position.X = distanceField.Width - 1; position.X >= 0; position.X--)
                {
                    SizeF currentDistance = distanceField[position];

                    foreach (Size offset in new[] { Right, Down, LeftDown, RightDown })
                    {
                        currentDistance = MinMagnitude(currentDistance, distanceField[position + offset] + offset);
                    }
                    distanceField[position] = currentDistance;
                }

                for (position.X = 0; position.X < distanceField.Width; position.X++)
                {
                    distanceField[position] = MinMagnitude(distanceField[position], distanceField[position + Left] + Left);
                }
            }
        }

        private static SizeF MinMagnitude(SizeF a, SizeF b)
        {
            return a.MagnitudeSquared() < b.MagnitudeSquared() ? a : b;
        }
    }
}
