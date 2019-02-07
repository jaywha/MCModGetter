using MCModGetter.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace MCModGetter.UserControls
{
    /// <summary>
    /// Interaction logic for ModChecklistControl.xaml
    /// </summary>
    public partial class ModChecklistControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public ObservableCollection<Mod> ModifyingModList = new ObservableCollection<Mod>();

        public ModChecklistControl()
        {
            InitializeComponent();

            ModifyingModList.Add(new Mod() { Name = "Test", Version = "v1.0", IsOnLocalMachine=true});
        }

        private void CheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            if(sender is CheckBox chkbx)
            {
                //TODO: Move checked boxes to rightside and unchecked to left side... right?
            }
        }
    }
}
