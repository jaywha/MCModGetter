using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
    /// Interaction logic for LoginControl.xaml
    /// </summary>
    public partial class LoginControl : UserControl, INotifyPropertyChanged
    {
        private string _email;
        public string Email {
            get => _email;
            set {
                _email = value;
                OnPropertyChanged();
            }
        }

        private string _password;
        public string Password {
            get => _password;
            set {
                _password = value;
                OnPropertyChanged();
            }
        }

        public LoginControl()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propFull = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propFull));

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            Password = txtPassword.Password;
            if(e.Key.Equals(Key.Enter))
            {
                IInvokeProvider invokeProv = new ButtonAutomationPeer(btnLogin).GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }
    }
}
