using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal Install Install => new Install(this);

        public BindingList<PluginEntry> Plugins { get; } = new BindingList<PluginEntry>();

        public MainWindow()
        {
            InitializeComponent();
            LoadPluginEntries();
            DataContext = this;
        }

        void LoadPluginEntries()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(".dll"))
                {
                    PluginEntry entry = new PluginEntry(resource, "C:\\Program Files\\paint.net\\Effects\\");
                    Plugins.Add(entry);
                }
            }
        }
    }

    class Install : ICommand
    {
        private readonly MainWindow window;

        internal Install(MainWindow window)
        {
            this.window = window;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            //window.pluginsGrid.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateSource();
        }
    }
}
