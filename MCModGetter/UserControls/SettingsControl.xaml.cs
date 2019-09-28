using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MCModGetter.Classes;

namespace MCModGetter.UserControls
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private string _configFileLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\.minecraft\config\";
        public string ConfigFileLocation
        {
            get => _configFileLocation;
            set
            {
                _configFileLocation = value;
                OnPropertyChanged();
            }
        }

        private string _previouslyEditedFile;
        public string PreviouslyEditedFile
        {
            get => _previouslyEditedFile;
            set {
                _previouslyEditedFile = value;
                OnPropertyChanged();
            }
        }


        private FileSystemWatcher fileSystemWatcher;

        public SettingsControl()
        {
            InitializeComponent();

            fileSystemWatcher = new FileSystemWatcher(ConfigFileLocation, "*.*")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            fileSystemWatcher.Created += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Changed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Renamed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Deleted += FileSystemWatcher_FileEvent;

            tvConfigs.ListDirectory(ConfigFileLocation);

            ConfigEditor.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition(Properties.Resources.JSONHighlighting);
        }

        #region FileSystemWatcher & TreeView Events
        private void FileSystemWatcher_FileEvent(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(() => {
                tvConfigs.Dispatcher.Invoke(() =>
                {
                    tvConfigs.ListDirectory(ConfigFileLocation);
                }, System.Windows.Threading.DispatcherPriority.Background);
            });
        }
        #endregion

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Process.Start(fileName: ConfigFileLocation);

        private void TvConfigs_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!string.IsNullOrEmpty(ConfigEditor.Text))
            {
                File.WriteAllText(ConfigFileLocation + PreviouslyEditedFile, ConfigEditor.Text);
            }

            if (tvConfigs.SelectedValue != null)
            {
                ConfigEditor.Text = File.ReadAllText(ConfigFileLocation + tvConfigs.SelectedValue.ToString());
                PreviouslyEditedFile = tvConfigs.SelectedValue.ToString();
            }
        }
    }
}
