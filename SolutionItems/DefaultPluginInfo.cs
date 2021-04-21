using PaintDotNet;
using System;
using System.Reflection;

namespace AssortedPlugins
{
    public class DefaultPluginInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://forums.getpaint.net/forum/7-plugins-publishing-only/");
    }
}
