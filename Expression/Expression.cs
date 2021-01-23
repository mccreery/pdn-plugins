using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using NReco.Linq;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace AssortedPlugins.Expression
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class Expression : PropertyBasedEffect
    {
        public enum PropertyNames
        {
            Program,
            LicenseLink
        }

        private static readonly decimal[] ByteToDecimal = new decimal[256];
        static Expression()
        {
            for (int i = 0; i < 256; i++)
            {
                ByteToDecimal[i] = i / 255.0m;
            }
        }

        private readonly LambdaParser lambdaParser;

        public Expression() : base(
            typeof(Expression).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(Expression), "icon.png"),
            "Advanced",
            new EffectOptions() { Flags = EffectFlags.Configurable })
        {
            lambdaParser = new LambdaParser();
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Program, PropertyControlType.TextBox);
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.Multiline, true);
            configUI.SetPropertyControlValue(PropertyNames.Program, ControlInfoPropertyNames.Description,
                "Write 1-4 expressions separated by newlines. Each line corresponds to an output channel depending on the number of lines:\r\n" +
                "\t\u2022 1 \u2192 RGB\r\n\t\u2022 2 \u2192 RGB, A\r\n\t\u2022 3 \u2192 R, G, B\r\n\t\u2022 4 \u2192 R, G, B, A\r\n\r\n" +
                "The variables r, g, b, a and x (the current channel) are provided.");

            configUI.SetPropertyControlType(PropertyNames.LicenseLink, PropertyControlType.LinkLabel);
            configUI.SetPropertyControlValue(PropertyNames.LicenseLink, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.LicenseLink, ControlInfoPropertyNames.Description, "Parser license");

            return configUI;
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new StringProperty(PropertyNames.Program, "x"));
            props.Add(new UriProperty(PropertyNames.LicenseLink, new Uri("https://not.real/")));

            return new PropertyCollection(props);
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            base.OnCustomizeConfigUIWindowProperties(props);
            props[ControlInfoPropertyNames.WindowTitle].Value = typeof(Expression).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }

        // Expressions for each channel in BGRA order (matching ColorBgra)
        private readonly string[] expressions = new string[4];

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            string program = newToken.GetProperty<StringProperty>(PropertyNames.Program).Value;
            string[] lines = program.Split(new char[] { '\r', '\n' }, 4, StringSplitOptions.RemoveEmptyEntries);

            switch (lines.Length)
            {
                case 1:
                    expressions[0] = expressions[1] = expressions[2] = lines[0];
                    expressions[3] = null;
                    break;
                case 2:
                    expressions[0] = expressions[1] = expressions[2] = lines[0];
                    expressions[3] = lines[1];
                    break;
                case 3:
                    // Reorder RGB to BGR
                    expressions[0] = lines[2];
                    expressions[1] = lines[1];
                    expressions[2] = lines[0];
                    expressions[3] = null;
                    break;
                case 4:
                    // Reorder RGBA to BGRA
                    expressions[0] = lines[2];
                    expressions[1] = lines[1];
                    expressions[2] = lines[0];
                    expressions[3] = lines[3];
                    break;
                default:
                    Debug.WriteLine("Program must be 1-4 lines long excluding blank lines");
                    break;
                    //throw new ArgumentException("Program must be 1-4 lines long excluding blank lines");
            }
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            int endIndex = startIndex + length;

            try
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    Render(DstArgs.Surface, SrcArgs.Surface, renderRects[i]);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra srcColor = src[x, y];
                    IDictionary<string, object> vars = new Dictionary<string, object>()
                    {
                        ["r"] = ByteToDecimal[srcColor.R],
                        ["g"] = ByteToDecimal[srcColor.G],
                        ["b"] = ByteToDecimal[srcColor.B],
                        ["a"] = ByteToDecimal[srcColor.A]
                    };

                    ColorBgra dstColor = srcColor;
                    for (int i = 0; i < 4; i++)
                    {
                        if (expressions[i] != null)
                        {
                            vars["x"] = ByteToDecimal[srcColor[i]];
                            object expressionResult = lambdaParser.Eval(expressions[i], vars);

                            if (expressionResult is decimal channel)
                            {
                                // Reuse double utility method to clamp
                                dstColor[i] = DoubleUtil.ClampToByte(Math.Round((double)channel * 255.0));
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid result of type {expressionResult.GetType()}");
                            }
                        }
                    }
                    dst[x, y] = dstColor;
                }
            }
        }
    }
}
