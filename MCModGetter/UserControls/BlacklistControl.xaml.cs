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
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;

namespace MCModGetter.UserControls
{
    /// <summary>
    /// Interaction logic for BlacklistControl.xaml
    /// </summary>
    public partial class BlacklistControl : UserControl
    {
        public ObservableCollection<string> ServerSideBlacklist { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> ClientSideBlacklist { get; set; } = new ObservableCollection<string>();

        public BlacklistControl()
        {
            InitializeComponent();
        }

        private void uccBlacklistControl_Loaded(object sender, RoutedEventArgs e)
        {
            PrepData();
        }

        private void PrepData()
        {
            // TODO: Store Blacklist on Database?
            ServerSideBlacklist.Add("Mod Name [version]");
            ClientSideBlacklist.Add("Mod Label [v0.0.1]");
        }
    }
}
