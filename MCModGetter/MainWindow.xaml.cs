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
using System.Net;
using System.Web.Script.Serialization;
using MojangSharp.Endpoints;
using MojangSharp.Responses;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using WinSCP;
using System.Configuration;
using System.Security.Cryptography;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using System.Media;
using MCModGetter.UserControls;
using Google.Apis.Drive.v3;
using Google.Apis.Download;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Google.Apis.Util.Store;
using MCModGetter.Classes;
using MojangSharp;

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

        private double _googleDriveDownloadProgress;
        public double GoogleDriveDownloadProgress
        {
            get => _googleDriveDownloadProgress;
            set
            {
                _googleDriveDownloadProgress = value;
                OnPropertyChanged();
            }
        }

        private string LogDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\MCModGetter-Logs\";

        private AuthenticateResponse UserAuthCache;

        private readonly SettingsControl settings;
        private static UserCredential Credential;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Hide();
            Show();

            #region Google Drive API Pre-Init
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { DriveService.Scope.DriveReadonly },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            #endregion

            fileSystemWatcher = new FileSystemWatcher(ModFileLocation, "*.*")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            fileSystemWatcher.Created += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Changed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Renamed += FileSystemWatcher_FileEvent;
            fileSystemWatcher.Deleted += FileSystemWatcher_FileEvent;

            foreach (string fileName in Directory.EnumerateFiles(ModFileLocation).Select((s) => s.Substring(s.LastIndexOf('\\') + 1)))
            {
                CurrentModList.Add(fileName);
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
                CurrentModList.Add(file.Substring(file.LastIndexOf('\\') + 1));
                File.Copy(file, ModFileLocation + file.Substring(file.LastIndexOf('\\') + 1));
            }
        }
        #endregion

        #region Utilitiy Methods
        private SessionOptions sessionOptions = new SessionOptions()
        {
            Protocol = Protocol.Ftp,
            HostName = "192.99.21.157",
            UserName = "jayw685@gmail.com.5215",
            Password = "pt2T0gy68E"
        };

        [Obsolete("Old Google Drive method. Not in use.")]
        private static readonly BaseClientService.Initializer DriveServiceInit = new BaseClientService.Initializer()
        {
            ApiKey = "AIzaSyDo1fRXMAMHrFaXlazsFQ-VkYBM8BVKrEI",
            HttpClientInitializer = Credential,
            ApplicationName = AppDomain.CurrentDomain.FriendlyName
        };
        [Obsolete("Old Google Drive method. Not in use.")]
        private readonly DriveService driveService = new DriveService(DriveServiceInit);
        [Obsolete("Old Google Drive method. Not in use.")]
        private readonly string DWNLD_PATH = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\mods.xlsx";

        [Obsolete("Old Google Drive method. Not in use.")]
        public void GetDriveMods()
        {
            var fileId = "1ecUF8Wb_EgJTXfRqwjIuirIX5jN4F7HOfeB87fZk-Q4";
            var request = driveService.Files.Export(fileId, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var stream = new MemoryStream();
            progbarUpdateMods.Dispatcher.Invoke(() => progbarUpdateMods.Visibility = Visibility.Visible);

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            {
                                GoogleDriveDownloadProgress = progress.BytesDownloaded;
                                break;
                            }
                        case DownloadStatus.Completed:
                            {
                                Console.WriteLine("Download complete.");
                                btnUpdateMods.Dispatcher.Invoke(() => btnUpdateMods.IsEnabled = true);
                                stkProgress.Dispatcher.Invoke(() => stkProgress.Visibility = Visibility.Collapsed);

                                using (var fs = new FileStream(DWNLD_PATH, FileMode.OpenOrCreate))
                                {
                                    stream.WriteTo(fs);

                                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
                                    {
                                        reader.Read(); // skip the headers

                                        Console.WriteLine(">>\nCurrent Mod List Data {");
                                        while (reader.Read()
                                        && !string.IsNullOrWhiteSpace(reader.GetString(0)))
                                        {
                                            var name = reader.GetString(0);
                                            var link = reader.GetString(1);

                                            Console.WriteLine($"\t\"{name}\" : {link}");
                                        }
                                        Console.WriteLine("}\n<<");
                                    }

                                    File.Delete(DWNLD_PATH);
                                }
                                progbarUpdateMods.Dispatcher.Invoke(() => progbarUpdateMods.Visibility = Visibility.Hidden);
                                break;
                            }
                        case DownloadStatus.Failed:
                            {
                                Console.WriteLine("Download failed.");
                                Console.WriteLine($"Progress status details: {progress.Status}");
                                Console.WriteLine($"Progress status exception: {progress.Exception}");

                                btnUpdateMods.Dispatcher.Invoke(() => btnUpdateMods.IsEnabled = true);
                                stkProgress.Dispatcher.Invoke(() => stkProgress.Visibility = Visibility.Collapsed);
                                progbarUpdateMods.Dispatcher.Invoke(() => progbarUpdateMods.Visibility = Visibility.Hidden);
                                break;
                            }
                    }
                };
            request.Download(stream);
        }

        private string remotePath = "/mods/";

        public void ProbeFiles()
        {
            try
            {
                using (Session session = new Session())
                {
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

                        if (!CurrentModList.Contains(fileInfo.Name))
                        {                            
                            if (fileInfo.Name.Contains("ChickenASM"))
                            {
                                var versionFolder = "1.12.2";
                                Toast.Dispatcher.Invoke(() =>
                                Toast.MessageQueue.Enqueue($"Downloading file into {versionFolder}\\{fileInfo.FullName}..."));
                                // Download file
                                string remoteFilePath = RemotePath.EscapeFileMask(fileInfo.FullName);
                                TransferOperationResult transferResult =
                                    session.GetFiles(remoteFilePath, localFilePath);

                                // Did the download succeeded?
                                if (!transferResult.IsSuccess)
                                {
                                    // Print error (but continue with other files)
                                    Toast.Dispatcher.Invoke(() =>
                                    Toast.MessageQueue.Enqueue($"Error downloading file {fileInfo.Name}: {transferResult.Failures[0].Message}"));
                                }
                            }
                            else
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
                                    Toast.Dispatcher.Invoke(() =>
                                    Toast.MessageQueue.Enqueue($"Error downloading file {fileInfo.Name}: {transferResult.Failures[0].Message}"));
                                }
                            }
                        }
                    }

                    Toast.Dispatcher.Invoke(() =>
                    Toast.MessageQueue.Enqueue("All mods up to date!"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                return;
            }
            finally
            {
                wndMain.Dispatcher.Invoke(() =>
                {
                    btnUpdateMods.IsEnabled = true;
                    progbarUpdateMods.Visibility = Visibility.Hidden;
                });
            }
        }

        private async void DialogHost_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
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
    }
}
