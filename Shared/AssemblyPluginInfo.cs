using PaintDotNet;
using System;
using System.Reflection;

namespace AssortedPlugins
{
    public class AssemblyPluginInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            }
        }

        public string Copyright
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            }
        }

        public string DisplayName
        {
            get
            {
                return base.GetType().Assembly.GetName().Name;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://forums.getpaint.net/forum/7-plugins-publishing-only/");
            }
        }
    }
}
