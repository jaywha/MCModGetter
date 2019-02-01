﻿using System;
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
using System.Net;
using System.Web.Script.Serialization;
using MojangSharp.Endpoints;
using MojangSharp.Responses;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using WinSCP;
using System.Configuration;
using System.Security.Cryptography;

namespace MCModGetter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private FileSystemWatcher fileSystemWatcher;
        public ObservableCollection<string> CurrentModList { get; private set; } = new ObservableCollection<string>();

        private string _modFileLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\.minecraft\mods\";
        public string ModFileLocation
        {
            get => _modFileLocation;
            set
            {
                _modFileLocation = value;
                OnPropertyChanged();
            }
        }

        private AuthenticateResponse UserAuthCache;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSessionsOptions();

            fileSystemWatcher = new FileSystemWatcher(ModFileLocation, "*.*")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            fileSystemWatcher.Created += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Changed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Renamed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Deleted += FileSystemWatcher_FileEvent;

            foreach (string fileName in Directory.EnumerateFiles(ModFileLocation,"*.*",SearchOption.AllDirectories).Select((s) => s.Substring(s.LastIndexOf('\\') + 1)))
            {
                CurrentModList.Add(fileName);
            }
        }

        #region FileSystemWatcher & TreeView Events
        private void FileSystemWatcher_FileEvent(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(() => {
                tvMods.Dispatcher.Invoke(() =>
                {
                    CurrentModList.Clear();
                    foreach (string fileName in Directory.EnumerateFiles(ModFileLocation).Select((s) => s.Substring(s.LastIndexOf('\\') + 1)))
                    {
                        CurrentModList.Add(fileName);
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            });
        }

        private void tvMods_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var f in files)
                {
                    CurrentModList.Add(f.Substring(f.LastIndexOf('\\') + 1));
                    File.Copy(f, ModFileLocation + f.Substring(f.LastIndexOf('\\') + 1));
                }
            }
        }

        private void tvMods_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        #endregion

        #region Expander Menu Item Clicks
        private void ExpanderMenuItem_LoginClick(object sender, EventArgs e) {
            IInvokeProvider invokeProv = new ButtonAutomationPeer(btnOpenDialog).GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
        }

        private void ExpanderMenuItem_SettingsClick(object sender, EventArgs e) =>
            Toast.MessageQueue.Enqueue("Work in progress...");
        #endregion

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Process.Start(ModFileLocation);

        private void BtnPlayMinecraft_Click(object sender, RoutedEventArgs e) {
            var p = Process.Start(@"C:\Program Files (x86)\Minecraft\MinecraftLauncher.exe");
            Hide();
            while (!p.HasExited) { /*spin*/ }
            Show();
            BringIntoView();
            Focus();
            Activate();
        }

        #region Utilitiy Methods
        private SessionOptions sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = "192.99.21.157",
            UserName = ConfigurationManager.AppSettings["UserName"]
        };

        private string remotePath = "/mods/";

        public void InitializeSessionsOptions()
        {
            string hex = ConfigurationManager.AppSettings["Password"];
            byte[] bytes =
                Enumerable.Range(0, hex.Length / 2).
                    Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16)).ToArray();
            byte[] decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            sessionOptions.Password = Encoding.Unicode.GetString(decrypted);
        }

        public void ProbeFiles()
        {
            try {
                using (Session session = new Session()) {
                    // Connect
                    session.Open(sessionOptions);

                    // Enumerate files and directories to download
                    IEnumerable<RemoteFileInfo> fileInfos =
                        session.EnumerateRemoteFiles(
                            remotePath, null,
                            EnumerationOptions.EnumerateDirectories |
                                EnumerationOptions.AllDirectories);

                    foreach (RemoteFileInfo fileInfo in fileInfos.OrderBy(fileInfo => fileInfo.Name))
                    {
                        string localFilePath = RemotePath.TranslateRemotePathToLocal(fileInfo.FullName, remotePath, ModFileLocation);

                        if (fileInfo.IsDirectory)
                        {
                            // Create local subdirectory, if it does not exist yet
                            if (!Directory.Exists(localFilePath)) Directory.CreateDirectory(localFilePath);
                            continue;
                        }

                        if (!CurrentModList.Contains(fileInfo.Name)
                            && MessageBox.Show($"Missing mod detected from server {fileInfo.Name}!\nWant to download it?", "Download Missing Mod?",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Toast.Dispatcher.Invoke(() =>
                            Toast.MessageQueue.Enqueue($"Downloading file {fileInfo.FullName}..."));
                            // Download file
                            string remoteFilePath = RemotePath.EscapeFileMask(fileInfo.FullName);
                            TransferOperationResult transferResult =
                                session.GetFiles(remoteFilePath, localFilePath);

                            // Did the download succeeded?
                            if (!transferResult.IsSuccess)
                            {
                                // Print error (but continue with other files)
                                Toast.Dispatcher.Invoke(()=>
                                Toast.MessageQueue.Enqueue($"Error downloading file {fileInfo.Name}: {transferResult.Failures[0].Message}"));
                            }
                        }
                    }
                    
                    Toast.Dispatcher.Invoke(()=>
                    Toast.MessageQueue.Enqueue("All mods up to date!"));
                }
            } catch(Exception e) {
                Console.WriteLine("Error: {0}", e);
                return;
            } finally {
                wndMain.Dispatcher.Invoke(() =>
                {
                    btnUpdateMods.IsEnabled = true;
                    progbarUpdateMods.Visibility = Visibility.Hidden;
                });
            }
        }    
        #endregion

        private async void DialogHost_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
            AuthenticateResponse auth = await new Authenticate(new Credentials() { Username = Account.Email, Password = Account.Password }).PerformRequestAsync();
            if (auth.IsSuccess) {
                UserAuthCache = auth;
                Console.WriteLine($"AccessToken: {auth.AccessToken}");
                Console.WriteLine($"ClientToken: {auth.ClientToken}");
                lblAccessToken.Content = auth.AccessToken;
                lblClientToken.Content = auth.ClientToken;
                emiLogin.Label = Account.Email;
                emiLogin.IsEnabled = false;
            } else {
                Toast.MessageQueue.Enqueue("Couldn't login you in. Check your login stuff and try again.");
            }
        }

        private void BtnDeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (tvMods.SelectedItem == null || MessageBox.Show($"Are you sure you want to delete this mod: {tvMods.SelectedValue.ToString()}?", 
                "Delete Mod Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning)==MessageBoxResult.No) return;

            var modName = tvMods.SelectedValue.ToString();
            File.Delete(ModFileLocation + modName);

            Toast.MessageQueue.Enqueue($"Successfully deleted the following mod: {modName}");
        }

        private async void btnUpdateMods_Click(object sender, RoutedEventArgs e) {
            btnUpdateMods.IsEnabled = false;
            progbarUpdateMods.Visibility = Visibility.Visible;
            await Task.Factory.StartNew(() => ProbeFiles());
        }
    }
}
