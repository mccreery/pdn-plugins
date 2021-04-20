using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using NReco.Linq;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

using LinqExpression = System.Linq.Expressions.Expression;

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
        //private readonly string[] expressions = new string[4];

        // Known type at compile time allows for much quicker invocation over DynamicInvoke
        // Arguments constitute the entire environment for expressions
        delegate float ChannelOp(float r, float g, float b, float a, float c);

        // Default ChannelOp
        private static float Identity(float r, float g, float b, float a, float c)
        {
            return c;
        }

        private static ParameterExpression[] GetParameters(Type delegateType)
        {
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            List<ParameterExpression> parameters = new List<ParameterExpression>();

            foreach (ParameterInfo parameter in invokeMethod.GetParameters())
            {
                parameters.Add(LinqExpression.Parameter(parameter.ParameterType, parameter.Name));
            }
            return parameters.ToArray();
        }

        // Operations for each channel in BGRA order to match ColorBgra
        private readonly ChannelOp[] ChannelOps = new ChannelOp[4];

        public static float Unwrap(ILambdaValue value) => (float)value.Value;

        class UnwrapVisitor : ExpressionVisitor
        {
            public override LinqExpression Visit(LinqExpression node)
            {
                if (node is ILambdaValue lambdaValue)
                {
                    return Visit((LinqExpression)lambdaValue.Value);
                }
                else
                {
                    return base.Visit(node);
                }
            }
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            // Reset channel ops
            for (int i = 0; i < 4; i++)
            {
                ChannelOps[i] = Identity;
            }

            string program = newToken.GetProperty<StringProperty>(PropertyNames.Program).Value;
            string[] lines = program.Split(new char[] { '\r', '\n' }, 4, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    LinqExpression expression = lambdaParser.Parse(lines[i]);
                    expression = new UnwrapVisitor().Visit(expression);

                    expression = LinqExpression.Convert(expression, typeof(float), typeof(Expression).GetMethod(nameof(Unwrap)));

                    //ParameterExpression[] parameters = LambdaParser.GetExpressionParameters(expression);

                    Expression<ChannelOp> lambdaExpression = LinqExpression.Lambda<ChannelOp>(expression, GetParameters(typeof(ChannelOp)));
                    ChannelOps[i] = lambdaExpression.Compile();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            // Temporary to swap RGB to BGR
            ChannelOp redOp;
            switch (lines.Length)
            {
                case 1:
                    // RGB single expression
                    ChannelOps[1] = ChannelOps[2] = ChannelOps[0];
                    break;
                case 2:
                    // RGB, A expressions
                    ChannelOps[3] = ChannelOps[1];
                    ChannelOps[1] = ChannelOps[2] = ChannelOps[0];
                    break;
                case 3:
                    // Individual R, G, B expressions

                    // Reorder RGB to BGR
                    redOp = ChannelOps[0];
                    ChannelOps[0] = ChannelOps[2];
                    ChannelOps[2] = redOp;
                    break;
                case 4:
                    // Individual R, G, B, A expressions

                    // Reorder RGB to BGR
                    redOp = ChannelOps[0];
                    ChannelOps[0] = ChannelOps[2];
                    ChannelOps[2] = redOp;
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
                    //IDictionary<string, object> vars = new Dictionary<string, object>()
                    //{
                    //    ["r"] = ByteToDecimal[srcColor.R],
                    //    ["g"] = ByteToDecimal[srcColor.G],
                    //    ["b"] = ByteToDecimal[srcColor.B],
                    //    ["a"] = ByteToDecimal[srcColor.A]
                    //};

                    ColorBgra dstColor = srcColor;
                    for (int i = 0; i < 4; i++)
                    {
                        float r = ByteUtil.ToScalingFloat(srcColor.R);
                        float g = ByteUtil.ToScalingFloat(srcColor.G);
                        float b = ByteUtil.ToScalingFloat(srcColor.B);
                        float a = ByteUtil.ToScalingFloat(srcColor.A);
                        float c = ByteUtil.ToScalingFloat(dstColor[i]);

                        float expressionResult = ChannelOps[i](r, g, b, a, c);
                        dstColor[i] = DoubleUtil.ClampToByte((double)Math.Round(expressionResult * 255.0));

                        //if (expressions[i] != null)
                        //{
                        //    vars["x"] = ByteToDecimal[srcColor[i]];
                        //    object expressionResult = lambdaParser.Eval(expressions[i], vars);

                        //    if (expressionResult is decimal channel)
                        //    {
                        //        // Reuse double utility method to clamp
                        //        dstColor[i] = DoubleUtil.ClampToByte(Math.Round((double)channel * 255.0));
                        //    }
                        //    else
                        //    {
                        //        throw new ArgumentException($"Invalid result of type {expressionResult.GetType()}");
                        //    }
                        //}
                    }
                    dst[x, y] = dstColor;
                }
            }
        }

        static decimal Lerp(decimal a, decimal b, decimal t)
        {
            return (1 - t) * a + t * b;
        }
    }
}
