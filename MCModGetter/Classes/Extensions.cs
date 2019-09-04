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
            var work = new Thread(() =>
            {

                var directoryNode = new TreeViewItem { Header = directoryInfo.Name };
                foreach (var directory in directoryInfo.GetDirectories())
                    directoryNode.Items.Add(treeView.CreateDirectoryNode(directory));

                foreach (var file in directoryInfo.GetFiles())
                    directoryNode.Items.Add(new TreeViewItem { Header = file.Name });

                result = directoryNode;
            });
            work.SetApartmentState(ApartmentState.STA);
            work.Start();
            work.Join(3000);
            return result;
        }


    }
}
