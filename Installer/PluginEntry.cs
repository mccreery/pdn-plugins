using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Windows;

namespace Installer
{
    public class PluginEntry
    {
        private readonly string resourceName;

        public string FileName { get; }
        public string Description { get; }
        internal string TargetDir { get; set; }

        internal static readonly DependencyProperty TargetDirProperty = DependencyProperty.Register(
            nameof(TargetDir), typeof(string), typeof(PluginEntry));

        private string TargetPath => Path.Combine(TargetDir, FileName);

        public bool Installed
        {
            get => File.Exists(TargetPath);
            set
            {
                if (value && !Installed)
                {
                    using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream(resourceName),
                        file = new FileStream(TargetPath, FileMode.Create))
                    {
                        stream.CopyTo(file);
                    }
                }
                else if (!value && Installed)
                {
                    File.Delete(TargetPath);
                }
            }
        }

        public PluginEntry()
        {
        }

        internal PluginEntry(string resourceName, string targetDir)
        {
            this.resourceName = resourceName;

            FileName = string.Join('.', resourceName.Split('.').TakeLast(2));
            Description = string.Empty;
            TargetDir = targetDir;
        }
    }
}
