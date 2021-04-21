using System.Drawing;
using System.Reflection;
using PaintDotNet;
using PaintDotNet.Effects;

namespace AssortedPlugins.RemoveTransparency
{
    [PluginSupportInfo(typeof(DefaultPluginInfo))]
    public class RemoveTransparency : Effect
    {
        public RemoveTransparency() : base(
            typeof(RemoveTransparency).Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title,
            new Bitmap(typeof(RemoveTransparency), "icon.png"),
            SubmenuNames.Render,
            new EffectOptions())
        {
        }

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = rois[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        dstArgs.Surface[x, y] = srcArgs.Surface[x, y].NewAlpha(255);
                    }
                }
            }
        }
    }
}
