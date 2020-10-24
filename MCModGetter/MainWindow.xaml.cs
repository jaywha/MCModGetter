using System;
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
using System.Reflection;
using System.Windows.Controls;

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
        public List<Mod> ServerModList { get; private set; } = new List<Mod>();

        private const string BaseURL = "ftp://144.217.65.175/mods/";
        private const string BaseTitle = "Minecraft Mod Manager";

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
            set
            {
                ftpDownloadProgress = value;
                OnPropertyChanged();
            }
        }

        private NetworkCredential VoodooCredential = new NetworkCredential("jayw685@gmail.com.6857", DBConfig.FakePebble);

        private StreamWriter CurrentLog;
        private string LogDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\MCModGetter-Logs\";
        private bool UpdateRanOnce = false;

        private AuthenticateResponse UserAuthCache;

        private readonly SettingsControl settings;
        private readonly BlacklistControl blacklist;
        #endregion

        public void SetupErrorCatchers()
        {
            // attempted to fix visual studio exception on stopping debugging
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                delegate (object sender, UnhandledExceptionEventArgs uaeargs) {
                    Exception e = (Exception)uaeargs.ExceptionObject;
                    var out_msg = $"[UnhandledExceptionHandler]: {e.Message}\n" +
                        $"(Stack Trace)\n{new string('-', 20)}\n\n{e.StackTrace}\n\n{new string('-', 20)}\n" +
                        $"Will runtime terminate now? -> \'{(uaeargs.IsTerminating ? "Yes" : "No")}\'";

                    MessageBox.Show(out_msg, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);

                });
            AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(
                delegate (object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs fcargs)
                {
                    Exception e = fcargs.Exception;
                    var out_msg = $"[FirstChanceHandler]: {e.Message}\n" +
                        $"(Stack Trace)\n{new string('-', 20)}\n\n{e.StackTrace}\n";

                    MessageBox.Show(out_msg, "First Chance Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                });
        }

        #region Window Methods

        public MainWindow()
        {
            SetupErrorCatchers();
            InitializeComponent();
            Hide();
            Show();

            try
            {
                Directory.CreateDirectory(ModFileLocation); // ensure mod folder exists
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong while creating the default mod folder.\nScreenshot this and let Jay know.\n Exception Message: {ex.Message}\n Stack Trace: {ex.StackTrace} ", "Mod Folder Error");
            }

            fileSystemWatcher = new FileSystemWatcher(ModFileLocation, "*.*")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            fileSystemWatcher.SetListeners(FileSystemWatcher_FileEvent);

            InitModList();

            try { 
                Directory.CreateDirectory(LogDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong while creating the error log folder.\nScreenshot this and let Jay know.\n Exception Message: {ex.Message}\n Stack Trace: {ex.StackTrace} ", "Log Folder Error");
            }

            settings = new SettingsControl() { Width = ActualWidth - 100.0, Height = ActualHeight - 100.0 };
            blacklist = new BlacklistControl() { Width = ActualWidth - 100.0, Height = ActualHeight - 100.0 };
        }

        private void InitModList()
        {
            try { 
                foreach (string dirName in Directory.EnumerateDirectories(ModFileLocation).Select(d => d.Substring(d.LastIndexOf('\\') + 1)))
                {
                    CurrentModList.Add(new Mod() { Name = dirName, ListViewIcon = new BitmapImage(new Uri("Images/folder-icon.png", UriKind.Relative)) });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong while reading all the mods in the mod folder.\nScreenshot and let Jay know.\n Exception Message: {ex.Message}\n Stack Trace: {ex.StackTrace} ", "Unknown Error");
            }

            try { 
                foreach (string fileName in Directory.EnumerateFiles(ModFileLocation).Select((s) => s.Substring(s.LastIndexOf('\\') + 1)))
                {
                    if (UpdateRanOnce 
                        && ServerModList.Select(m => m.Name.Equals(fileName) ? m : null).Single() == null
                        && File.Exists(ModFileLocation + fileName))
                    {
                        File.Delete(ModFileLocation + fileName);
                    }

                    CurrentModList.Add(new Mod() { Name = fileName });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong while reading all the sub-folders in the mod folder.\nScreenshot and let Jay know.\n Exception Message: {ex.Message}\n Stack Trace: {ex.StackTrace} ", "Unknown Error");
            }
        }

        private void WndMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(@"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe"))
            {
                lblInstallMCText.Content = "Minecraft Detected!";
                packInstallMCIcon.Kind = PackIconKind.Check;
                btnInstallMinecraft.IsEnabled = false;
            }

            if (File.Exists($@"C:\Users\{Environment.UserName}\AppData\Roaming\.minecraft\libraries\net\minecraftforge\forge\1.12.2-14.23.5.2838\forge-1.12.2-14.23.5.2838.jar"))
            {
                lblInstallForgeText.Content = "Forge Deteced!";
                packInstallForgeIcon.Kind = PackIconKind.Check;
                btnInstallForge.IsEnabled = false;
            }
        }

        private void WndMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (dlghostMain.IsOpen)
            {
                settings.Width = ActualWidth - 100.0;
                settings.Height = ActualHeight - 100.0;
            }
        }

        private void wndMain_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void wndMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Visual visual = e.OriginalSource as Visual;

            if (!visual.IsDescendantOf(expSideMenu))
                expSideMenu.IsExpanded = false;
        }
        #endregion

        #region FileSystemWatcher & TreeView Events
        private void FileSystemWatcher_FileEvent(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                tvMods.Dispatcher.Invoke(() =>
                {
                    CurrentModList.Clear();
                    foreach (string dirName in Directory.EnumerateDirectories(ModFileLocation).Select(d => d.Substring(d.LastIndexOf('\\') + 1)))
                    {
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
        private void ExpanderMenuItem_LoginClick(object sender, EventArgs e)
        {
            DialogHost.Show(new LoginControl());
            expSideMenu.IsExpanded = false;
        }

        private Window settingsWindow;
        private void ExpanderMenuItem_ConfigsClick(object sender, EventArgs e)
        {
            if (settingsWindow != null)
            {
                settingsWindow.CenterWindowOnScreen();
                settingsWindow.Activate();
            }
            else
            {
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

        private Window blacklistWindow;
        private void emiClientModBlacklist_ExpanderItemClick(object sender, EventArgs e)
        {
            if (blacklistWindow != null)
            {
                blacklistWindow.CenterWindowOnScreen();
                blacklistWindow.Activate();
            }
            else
            {
                blacklistWindow = new Window()
                {
                    Title = "Mod Blacklist Settings Window",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = blacklist,
                    WindowStyle = WindowStyle.ToolWindow
                };
                blacklistWindow.Show();
            }
            expSideMenu.IsExpanded = false;
        }
        #endregion

        #region Button Clicks
        private const string MinecraftLauncherDirectory = @"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe";

        private void btnInstallMinecraft_Click(object sender, RoutedEventArgs e)
        {
            // TODO: (Lizzie) Auto install the correct version of Minecraft
            // 1. Download the correct Minecraft installation file (possibly from our Google drive?)
            // 2. Run the installation file (could be a .exe or .jar, can't remember)
            // 3. Verify that the .minecraft folder exists in C:\Users\<Username>\AppData\Roaming\
            Toast.MessageQueue.Enqueue("Install MC - Work In Progress");
        }

        private void btnInstallForge_Click(object sender, RoutedEventArgs e)
        {
            // TODO: (Lizzie) Auto install the correct version of Forge
            // 1. Download the correct Forge installation file (possibly from our Google drive?)
            // 2. Run the installation file (could be a .exe or .jar, can't remember)
            // 3. Run the game once and close it once title screen hits
            // 4. Verify that the mods folder exists in C:\Users\<Username>\AppData\Roaming\.minecraft\
            Toast.MessageQueue.Enqueue("Install Forge - Work In Progress");
        }

        private void mnuiRefreshMods_Click(object sender, RoutedEventArgs e)
        {
            CurrentModList.Clear();
            InitModList();
        }

        private void BtnPlayMinecraft_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            var p = Process.Start(MinecraftLauncherDirectory);
            p.Exited += delegate
            {
                Process.GetProcessesByName("javaw.exe")[0].WaitForExit();

                Show();
                BringIntoView();
                Focus();
                Activate();
            };
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
            UpdateRanOnce = true;
            Notify.ShowBalloonTip("Start MC Mod Updates", "We're updating mods now. This should take just a couple of minutes...", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            fileSystemWatcher.UnsetListeners(FileSystemWatcher_FileEvent);

            try
            {
                btnUpdateMods.IsEnabled = false;
                stkProgress.Visibility = Visibility.Visible;
                FTPDownloadProgress = 0.0;
                //Directory.Delete(ModFileLocation.Substring(0, ModFileLocation.Length - 1), true);
                Directory.CreateDirectory(ModFileLocation);

                CurrentLog = File.AppendText(LogDirectory + $"UpdateMods_{DateTime.Now:MM-dd-yyyy hhmmss}");
                await Task.Factory.StartNew(() => ProbeFiles())
                    .ContinueWith(prev =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            CurrentModList.Clear();
                            InitModList();
                        });
                    });
            }
            catch (IOException ioe)
            {
                Toast.MessageQueue.Enqueue("Minecraft or some other program is blocking access to mod files!", "Task Manager", delegate
                {
                    Process.GetProcessesByName("taskmgr.exe");
                });
            }

            fileSystemWatcher.SetListeners(FileSystemWatcher_FileEvent);
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

                while (!reader.EndOfStream)
                {
                    var newFile = reader.ReadLine().Split(' ');
                    if (newFile.First().StartsWith("d"))
                    {
                        Files.Add("DIR" + newFile.Last());
                    }
                    else Files.Add(newFile.Last());
                }

                Console.WriteLine($"Directory List Complete, status: {response.StatusDescription}");
                Console.WriteLine("=== Start Listing ===");

                reader.Close();
                response.Close();

                var filesDownloaded = 0;
                foreach (var file in Files)
                {
                    string localFilePath = ModFileLocation + innerPath + file;
                    string remoteFilePath = currentURL + file;

                    Console.Write($"File [{file}] ==> ");

                    Mod serverMod = new Mod() { Name = file, IsClientSide = false };

                    if ((file.StartsWith("DIR") && !Directory.Exists(localFilePath)) || !File.Exists(localFilePath))
                    {
                        Console.WriteLine("Downloaded!");

                        if (file.StartsWith("DIR"))
                        {
                            if (!Directory.Exists(localFilePath)) Directory.CreateDirectory(localFilePath);

                            try
                            {
                                ProbeFiles(innerPath + file.Substring(3) + "/");
                            }
                            catch (Exception e)
                            {
                                // Print error (but continue with other files)
                                CurrentLog.WriteLine($"Error enumerating folder {file}!");
                            }
                        }
                        else // is file
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
                            else
                            {
                                serverMod.IsOnLocalMachine = true;
                                ServerModList.Add(serverMod);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Skipped...");
                    }

                    FTPDownloadProgress = (filesDownloaded++ / (double)Files.Count()) * 100;
                    Dispatcher.Invoke(() => Title = BaseTitle + " - " + (FTPDownloadProgress / 100).ToString("P"));
                }

                Dispatcher.Invoke(() => Title = BaseTitle);
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
                        Notify.ShowBalloonTip("Finished MC Mod Updates!", "Updates are done! You can now play on the server.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                        CurrentLog.Flush();
                        CurrentLog.Close();
                        btnUpdateMods.IsEnabled = true;
                        stkProgress.Visibility = Visibility.Hidden;
                        tvMods.InvalidateArrange();
                    });
                }
            }
        }

        private void DownloadMod(string remoteURL, string localFilePath)
        {
            string trueLocal = localFilePath.Replace('/', '\\');
            string dirPath = trueLocal.Substring(0, trueLocal.LastIndexOf('\\') + 1);

            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteURL);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = VoodooCredential;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Directory.CreateDirectory(trueLocal.Substring(0, trueLocal.LastIndexOf('\\') + 1));

            using (Stream localFile = File.OpenWrite(trueLocal))
            using (Stream responseStream = response.GetResponseStream())
            {
                responseStream.CopyTo(localFile);
            }
        }

        private async void DialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter != null && eventArgs.Parameter.ToString().Equals("LOGIN"))
            {
                AuthenticateResponse auth = await new Authenticate(
                    new Credentials()
                    {
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

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var snd = "tmp.wav";
            if (!File.Exists(snd))
            {
                Assembly assembly = Assembly.GetCallingAssembly();

                using (BinaryReader r = new BinaryReader(Properties.Resources.emgei))
                using (FileStream fs = new FileStream(snd, FileMode.OpenOrCreate))
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    w.Write(r.ReadBytes((int)Properties.Resources.emgei.Length));
                }
            }

            var soundEffect = new MediaPlayer();
            soundEffect.Open(new Uri(snd, UriKind.Relative));
            soundEffect.Volume = 0.4;
            soundEffect.Play();
        }
        #endregion

        private void Notify_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            BringIntoView();
            Activate();
            Focus();
        }
    }
}
