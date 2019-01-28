using System;
using System.Collections.Generic;
using System.Linq;
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

namespace MCModGetter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ExpanderMenuItem_ExpanderItemClick(object sender, EventArgs e)
        {
            if(sender.GetType() == typeof(Label) || sender.GetType() == typeof(Image)) return;
            else MessageBox.Show($"This menu item works and is labelled as {(sender as ExpanderMenuItem).Label}", "Test Success!!!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
