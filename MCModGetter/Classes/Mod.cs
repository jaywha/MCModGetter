using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MCModGetter.Classes
{
    /// <summary>
    /// Data Model representing typical data about an MC Mod.
    /// </summary>
    public class Mod
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsClientSide { get; set; }
        public bool IsOnLocalMachine { get; set; }

        public ImageSource ListViewIcon { get; set; } = new BitmapImage(new Uri("Images/file.png", UriKind.Relative));

        public override string ToString() => Name;
        
    }
}
