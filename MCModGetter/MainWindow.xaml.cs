﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MojangSharp.Endpoints;
using MojangSharp.Responses;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using System.Media;
using MCModGetter.UserControls;
using MCModGetter.Classes;
using System.Threading;
using System.Net;
using System.Windows.Media.Imaging;

namespace MCModGetter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region PropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion

        #region Properties
        private FileSystemWatcher fileSystemWatcher;
        public ObservableCollection<Mod> CurrentModList { get; private set; } = new ObservableCollection<Mod>();

        private const string BaseURL = "ftp://144.217.65.175/mods/";

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

        private double ftpDownloadProgress = 0.0;
        public double FTPDownloadProgress
        {
            get => ftpDownloadProgress;
            set {
                ftpDownloadProgress = value;
                OnPropertyChanged();
            }
        }

        private NetworkCredential VoodooCredential = new NetworkCredential("jayw685@gmail.com.6857", "pt2T0gy68E");

        private StreamWriter CurrentLog;
        private string LogDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\MCModGetter-Logs\";

        private AuthenticateResponse UserAuthCache;

        private readonly SettingsControl settings;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Hide();
            Show();

            Directory.CreateDirectory(ModFileLocation); // ensure mod folder exists

            fileSystemWatcher = new FileSystemWatcher(ModFileLocation, "*.*")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            fileSystemWatcher.Created += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Changed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Renamed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Deleted += FileSystemWatcher_FileEvent;

            foreach (string dirName in Directory.EnumerateDirectories(ModFileLocation).Select(d => d.Substring(d.LastIndexOf('\\') + 1)))
            {
                CurrentModList.Add(new Mod() { Name = dirName, ListViewIcon = new BitmapImage(new Uri("Images/folder-icon.png", UriKind.Relative)) });
            }

            foreach (string fileName in Directory.EnumerateFiles(ModFileLocation).Select((s) => s.Substring(s.LastIndexOf('\\') + 1)))
            {
                CurrentModList.Add(new Mod() { Name = fileName });
            }

            Directory.CreateDirectory(LogDirectory);

            settings = new SettingsControl() { Width = ActualWidth - 100.0, Height = ActualHeight - 100.0 };
        }

        private void WndMain_Loaded(object sender, RoutedEventArgs e) { /*NOTE: Any preinit*/ }

        private void WndMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(dlghostMain.IsOpen)
            {
                settings.Width = ActualWidth - 100.0;
                settings.Height = ActualHeight - 100.0;
            }
        }

        private void wndMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Visual visual = e.OriginalSource as Visual;

            if (!visual.IsDescendantOf(expSideMenu))
                expSideMenu.IsExpanded = false;
        }

        #region FileSystemWatcher & TreeView Events
        private void FileSystemWatcher_FileEvent(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(() => {
                tvMods.Dispatcher.Invoke(() =>
                {
                    CurrentModList.Clear();
                    foreach (string dirName in Directory.EnumerateDirectories(ModFileLocation).Select(d => d.Substring(d.LastIndexOf('\\') + 1))) {
                        CurrentModList.Add(new Mod() { Name = dirName, ListViewIcon = new BitmapImage(new Uri("Images/folder-icon.png", UriKind.Relative)) });
                    }

                    foreach (string fileName in Directory.EnumerateFiles(ModFileLocation).Select(s => s.Substring(s.LastIndexOf('\\') + 1)))
                    {
                        CurrentModList.Add(new Mod() { Name = fileName });
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
                    CurrentModList.Add(new Mod() { Name = f.Substring(f.LastIndexOf('\\') + 1) });
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

         private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Process.Start(ModFileLocation);
        #endregion

        #region Expander Menu Item Clicks
        private void ExpanderMenuItem_LoginClick(object sender, EventArgs e) {
            DialogHost.Show(new LoginControl());
            expSideMenu.IsExpanded = false;
        }

        private Window settingsWindow;
        private void ExpanderMenuItem_SettingsClick(object sender, EventArgs e) {
            if (settingsWindow != null) {
                settingsWindow.CenterWindowOnScreen();
                settingsWindow.Activate();
            } else {
                settingsWindow = new Window()
                {
                    Title = "Config Settings Window",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = settings,
                    WindowStyle = WindowStyle.ToolWindow
                };
                settingsWindow.Show();
            }
            expSideMenu.IsExpanded = false;
        }
        #endregion

        #region Button Clicks
        private const string MinecraftLauncherDirectory = @"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe";

        private void BtnPlayMinecraft_Click(object sender, RoutedEventArgs e) {
            Hide();
            var p = Process.Start(MinecraftLauncherDirectory);
            p.Exited += delegate {
                Show();
                BringIntoView();
                Focus();
                Activate();
            };
            p.WaitForExit();
        }

        private void BtnDeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (tvMods.SelectedItem == null || MessageBox.Show($"Are you sure you want to delete this mod: {tvMods.SelectedValue.ToString()}?",
                "Delete Mod Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;

            var modName = tvMods.SelectedValue.ToString();
            File.Delete(ModFileLocation + modName);

            Toast.MessageQueue.Enqueue($"Successfully deleted the following mod: {modName}");
        }

        private async void btnUpdateMods_Click(object sender, RoutedEventArgs e)
        {
            btnUpdateMods.IsEnabled = false;
            stkProgress.Visibility = Visibility.Visible;
            FTPDownloadProgress = 0.0;
            Directory.Delete(ModFileLocation.Substring(0, ModFileLocation.Length-1), true);
            Directory.CreateDirectory(ModFileLocation);

            CurrentLog = File.AppendText(LogDirectory + $"UpdateMods_{DateTime.Now:MM-dd-yyyy hhmmss}");
            await Task.Factory.StartNew(() => ProbeFiles());
        }

        private void BtnAddMod_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "MC Mod Files (*.jar)|*.jar|All files (*.*)|*.*",
                InitialDirectory = $@"C:\Users\{Environment.UserName}\Downloads\",
                Title = "Add new Minecraft Mods...",
                Multiselect = true
            };

            ofd.ShowDialog();

            if (ofd.FileNames == null) return;

            foreach (string file in ofd.FileNames)
            {
                CurrentModList.Add(new Mod() { Name = file.Substring(file.LastIndexOf('\\') + 1) });
                File.Copy(file, ModFileLocation + file.Substring(file.LastIndexOf('\\') + 1));
            }
        }
        #endregion

        #region Utilitiy Methods

        public void ProbeFiles(string innerPath = "")
        {
            string currentURL = BaseURL + innerPath;

            try
            {
                // Enumerate files and directories to download
                List<string> Files = new List<string>();

                // Get the object used to communicate with the server.
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(currentURL);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                // This example assumes the FTP site uses anonymous logon.
                request.Credentials = VoodooCredential;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                while(!reader.EndOfStream)
                {
                    var newFile = reader.ReadLine().Split(' ');
                    if (newFile.First().StartsWith("d")) {
                        Files.Add("DIR" + newFile.Last());
                    } else Files.Add(newFile.Last());
                }

                Console.WriteLine($"Directory List Complete, status {response.StatusDescription}");

                reader.Close();
                response.Close();

                var filesDownloaded = 0;
                foreach (var file in Files)
                {
                    string localFilePath = ModFileLocation + innerPath + file;
                    string remoteFilePath = currentURL + file;

                    if (!CurrentModList.Where(mod=>!mod.Name.Equals(file)).Any())
                    {
                        if (file.StartsWith("DIR"))
                        {
                            try {
                                ProbeFiles(innerPath+file.Substring(3)+"/");
                            } catch(Exception e) {
                                // Print error (but continue with other files)
                                CurrentLog.WriteLine($"Error enumerating folder {file}!");
                            }
                        }
                        else
                        {
                            CurrentLog.WriteLine($"Downloading file {file}...");
                            // Download file
                            DownloadMod(remoteFilePath, localFilePath);

                            // Did the download succeeded?
                            if (!File.Exists(localFilePath))
                            {
                                // Print error (but continue with other files)
                                CurrentLog.WriteLine($"Error downloading file {file}!");
                            }
                        }
                    }

                    FTPDownloadProgress = (filesDownloaded++ / (double) Files.Count()) * 100;
                }

                CurrentLog.WriteLine("All mods up to date!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                return;
            }
            finally
            {
                if (string.IsNullOrWhiteSpace(innerPath))
                {
                    wndMain.Dispatcher.Invoke(() =>
                    {
                        btnUpdateMods.IsEnabled = true;
                        stkProgress.Visibility = Visibility.Hidden;
                        tvMods.InvalidateArrange();
                    });
                }
            }
        }

        private void DownloadMod(string remoteURL, string localFilePath)
        {
            var trueLocal = localFilePath.Replace('/', '\\');
            var dirPath = trueLocal.Substring(0, trueLocal.LastIndexOf('\\')+1);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteURL);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = VoodooCredential;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            using (Stream localFile = File.OpenWrite(localFilePath))
            using (Stream responseStream = response.GetResponseStream()) {
                responseStream.CopyTo(localFile);
            }
        }

        private async void DialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter != null && eventArgs.Parameter.ToString().Equals("LOGIN"))
            {
                AuthenticateResponse auth = await new Authenticate(
                    new Credentials() { 
                        Username = Account.Email, 
                        Password = Account.Password 
                    }).PerformRequestAsync();
                if (auth.IsSuccess)
                {
                    UserAuthCache = auth;
                    Console.WriteLine($"AccessToken: {auth.AccessToken}");
                    Console.WriteLine($"ClientToken: {auth.ClientToken}");
                    lblAccessToken.Content = auth.AccessToken;
                    lblClientToken.Content = auth.ClientToken;
                    emiLogin.Label = Account.Email;
                    emiLogin.IsEnabled = false;
                }
                else
                {
                    Toast.MessageQueue.Enqueue("Couldn't login you in. Check your login stuff and try again.");
                }
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e) => new SoundPlayer(MCModGetter.Properties.Resources.emgei).Play();
        #endregion

        private void btnInstallMinecraft_Click(object sender, RoutedEventArgs e)
        {
            // TODO: (Lizzie) Auto install the correct version of Minecraft
            // 1. Download the correct Minecraft installation file (possibly from our Google drive?)
            // 2. Run the installation file (could be a .exe or .jar, can't remember)
            // 3. Verify that the .minecraft folder exists in C:\Users\<Username>\AppData\Roaming\
        }

        private void btnInstallForge_Click(object sender, RoutedEventArgs e)
        {
            // TODO: (Lizzie) Auto install the correct version of Forge
            // 1. Download the correct Forge installation file (possibly from our Google drive?)
            // 2. Run the installation file (could be a .exe or .jar, can't remember)
            // 3. Run the game once and close it once title screen hits
            // 4. Verify that the mods folder exists in C:\Users\<Username>\AppData\Roaming\.minecraft\
        }
    }
}
