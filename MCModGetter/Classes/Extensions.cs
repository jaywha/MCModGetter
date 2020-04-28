using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MCModGetter.Classes
{
    public static class Extensions
    {
        [STAThread]
        public static void ListDirectory(this TreeView treeView, string path)
        {
            treeView.Dispatcher.Invoke(() => {
                treeView.Items.Clear();
                var rootDirectoryInfo = new DirectoryInfo(path);
                treeView.Items.Add(treeView.CreateDirectoryNode(rootDirectoryInfo));
            });
        }

        [STAThread]
        private static TreeViewItem CreateDirectoryNode(this TreeView treeView, DirectoryInfo directoryInfo)
        {
            TreeViewItem result = null;
            var directoryNode = new TreeViewItem { Header = directoryInfo.Name };
            foreach (var directory in directoryInfo.GetDirectories())
                directoryNode.Dispatcher.Invoke(()=>directoryNode.Items.Add(treeView.Dispatcher.Invoke(()=>treeView.CreateDirectoryNode(directory))));

            foreach (var file in directoryInfo.GetFiles())
                directoryNode.Dispatcher.Invoke(()=>directoryNode.Items.Add(new TreeViewItem { Header = file.Name }));

            result = directoryNode;
            return result;
        }

        [STAThread]
        public static void CenterWindowOnScreen(this Window wnd)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = wnd.Width;
            double windowHeight = wnd.Height;
            wnd.Left = (screenWidth / 2) - (windowWidth / 2);
            wnd.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        /// <summary>
        /// Will set the calling <see cref="FileSystemWatcher"/> create, change, rename, and delete events equal to the <see cref="FileSystemEventHandler"/> provided.
        /// </summary>
        /// <param name="watcher">The calling <see cref="FileSystemWatcher"/></param>
        /// <param name="handler">The <see cref="FileSystemEventHandler"/> for create, change, and delete.</param>
        /// <param name="handler2">The <see cref="RenamedEventHandler"/> for rename events (can be same as <paramref name="handler"/> just needs casting from method group to correct handler)</param>
        [STAThread]
        public static void SetListeners(this FileSystemWatcher watcher, FileSystemEventHandler handler, RenamedEventHandler handler2)
        {
            watcher.Created += handler;
            watcher.Changed += handler;
            watcher.Renamed += handler2;
            watcher.Deleted += handler;
        }

        /// <summary>
        /// Will unset the calling <see cref="FileSystemWatcher"/> create, change, rename, and delete events equal to the <see cref="FileSystemEventHandler"/> provided.
        /// </summary>
        /// <param name="watcher">The calling <see cref="FileSystemWatcher"/></param>
        /// <param name="handler">The <see cref="FileSystemEventHandler"/> for create, change, and delete.</param>
        /// <param name="handler2">The <see cref="RenamedEventHandler"/> for rename events (can be same as <paramref name="handler"/> just needs casting from method group to correct handler)</param>
        [STAThread]
        public static void UnsetListeners(this FileSystemWatcher watcher, FileSystemEventHandler handler, RenamedEventHandler handler2)
        {
            watcher.Created -= handler;
            watcher.Changed -= handler;
            watcher.Renamed -= handler2;
            watcher.Deleted -= handler;
        }
    }
}
