using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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


    }
}
