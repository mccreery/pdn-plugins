using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using ColorWheelControl = PaintDotNet.ColorBgra;
using IntSliderControl = System.Int32;
using RadioButtonControl = System.Byte;

namespace GrowEffect
{
    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Grow")]
    public class GrowEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "Grow";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return null;
            }
        }

        public static string SubmenuName
        {
            get
            {
                return SubmenuNames.Distort;
            }
        }

        public GrowEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        public enum PropertyNames
        {
            Radius,
            FillColor,
            CustomColor
        }

        public enum FillColorOptions
        {
            FillColorOption1,
            FillColorOption2,
            FillColorOption3
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            ColorBgra PrimaryColor = EnvironmentParameters.PrimaryColor.NewAlpha(byte.MaxValue);
            ColorBgra SecondaryColor = EnvironmentParameters.SecondaryColor.NewAlpha(byte.MaxValue);

            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Radius, 5, 1, 50));
            props.Add(StaticListChoiceProperty.CreateForEnum<FillColorOptions>(PropertyNames.FillColor, 0, false));
            props.Add(new Int32Property(PropertyNames.CustomColor, ColorBgra.ToOpaqueInt32(Color.Black), 0, 0xffffff));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Radius, ControlInfoPropertyNames.DisplayName, "Radius");
            configUI.SetPropertyControlValue(PropertyNames.FillColor, ControlInfoPropertyNames.DisplayName, "Color");
            configUI.SetPropertyControlType(PropertyNames.FillColor, PropertyControlType.RadioButton);
            PropertyControlInfo FillColorControl = configUI.FindControlForPropertyName(PropertyNames.FillColor);
            FillColorControl.SetValueDisplayName(FillColorOptions.FillColorOption1, "Primary");
            FillColorControl.SetValueDisplayName(FillColorOptions.FillColorOption2, "Secondary");
            FillColorControl.SetValueDisplayName(FillColorOptions.FillColorOption3, "Custom");
            configUI.SetPropertyControlValue(PropertyNames.CustomColor, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlType(PropertyNames.CustomColor, PropertyControlType.ColorWheel);

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "Grow";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Grow v0.1\nCopyright ©2019 by Sam McCreery\nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Radius = newToken.GetProperty<Int32Property>(PropertyNames.Radius).Value;
            FillColor = (byte)(int)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.FillColor).Value;
            CustomColor = ColorBgra.FromOpaqueInt32(newToken.GetProperty<Int32Property>(PropertyNames.CustomColor).Value);

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, rois[i]);
            }
        }

        #region User Entered Code
        // Name: Grow
        // Submenu: Distort
        // Author: Sam McCreery
        // Title: Grow
        // Version: 0.1
        // Desc: Grow a shape using alpha information from the image
        // Keywords:
        // URL:
        // Help:
        #region UICode
        IntSliderControl Radius = 5; // [1,50] Radius
        RadioButtonControl FillColor = 0; // Color|Primary|Secondary|Custom
        ColorWheelControl CustomColor = ColorBgra.FromBgr(0, 0, 0); // [Black]
        #endregion UICode

        ColorBgra GetColor()
        {
            switch (FillColor)
            {
                case 0:
                default: return EnvironmentParameters.PrimaryColor;
                case 1: return EnvironmentParameters.SecondaryColor;
                case 2: return CustomColor;
            }
        }

        double[,] GetKernel(double radius)
        {
            int center = (int)Math.Ceiling(radius);
            int size = center * 2 + 1;

            double[,] kernel = new double[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    double d = Math.Sqrt(dx * dx + dy * dy);

                    double fac = radius + 1 - d;
                    if (fac < 0) fac = 0;
                    else if (fac > 1) fac = 1;

                    // TODO debug
                    fac = Math.Round(fac);

                    kernel[y, x] = fac;
                }
            }
            return kernel;
        }

        ColorBgra ZeroPad(Surface src, int x, int y)
        {
            return src.Bounds.Contains(x, y) ? src[x, y] : ColorBgra.TransparentBlack;
        }

        byte ConvolveMaxAlpha(Surface src, int centerX, int centerY, double[,] kernel, int anchorX = -1, int anchorY = -1)
        {
            int kernelHeight = kernel.GetLength(0);
            int kernelWidth = kernel.GetLength(1);

            int kernelTop = centerY - (anchorY >= 0 ? anchorY : kernelHeight / 2);
            int kernelLeft = centerX - (anchorX >= 0 ? anchorX : kernelWidth / 2);

            byte maxAlpha = 0;
            for (int y = 0; y < kernelHeight; y++)
            {
                for (int x = 0; x < kernelWidth; x++)
                {
                    byte alpha = ZeroPad(src, kernelLeft + x, kernelTop + y).A;
                    alpha = (byte)Math.Round((double)alpha * kernel[y, x]);

                    maxAlpha = Math.Max(maxAlpha, alpha);
                }
            }
            return maxAlpha;
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            ColorBgra color = GetColor();
            double[,] kernel = GetKernel(Radius);

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = color.NewAlpha(ConvolveMaxAlpha(src, x, y, kernel));
                }
            }
        }

        #endregion User Entered Code
    }
}
