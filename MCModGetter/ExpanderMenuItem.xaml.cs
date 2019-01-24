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
    /// Interaction logic for ExpanderMenuItem.xaml
    /// </summary>
    public partial class ExpanderMenuItem : UserControl
    {

        private object @lockMenuItemClickEvent = new object();
        public static readonly DependencyProperty ExpanderItemClickProperty =
            DependencyProperty.Register("ExpanderItemClick", typeof(EventHandler), typeof(ExpanderMenuItem));

        /// <summary> EventHandler property <see cref="ExpanderItemClick"/> </summary>
        /// <remarks> To raise the event: ((EventHandler)GetValue(ExpanderItemClickProperty))?.Invoke(object sender, EventArgs e) </remarks>
        public event EventHandler ExpanderItemClick
        {
            add
            {
                lock (@lockMenuItemClickEvent)
                {
                    SetValue(ExpanderItemClickProperty, value);
                }
            }
            remove
            {
                lock (@lockMenuItemClickEvent)
                {
                    SetValue(ExpanderItemClickProperty, null);
                }
            }
        }


        public ExpanderMenuItem()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, MouseButtonEventArgs e) => ((EventHandler)GetValue(ExpanderItemClickProperty))?.Invoke(sender, e);
    }
}
