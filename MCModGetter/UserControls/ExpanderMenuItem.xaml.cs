using System;
using System.Collections.Generic;
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
    /// Interaction logic for ExpanderMenuItem.xaml
    /// </summary>
    public partial class ExpanderMenuItem : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(ExpanderMenuItem));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(ExpanderMenuItem), new PropertyMetadata("Label"));
        public static readonly DependencyProperty ExpanderItemClickProperty = DependencyProperty.Register("ExpanderItemClick", typeof(EventHandler), typeof(ExpanderMenuItem));

        #region Properties
        [Description("Left-aligned icon for this item."), Category("Common")]
        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); OnPropertyChanged(); }
        }

        [Description("The right-aligned text for this item."), Category("Common")]
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); OnPropertyChanged(); }
        }

        private object @lockMenuItemClickEvent = new object();

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] String propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        

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
        #endregion

        private Brush InitalBackground;

        public ExpanderMenuItem()
        {
            InitializeComponent();
            InitalBackground = Background;
        }

        private void MenuItem_Click(object sender, MouseButtonEventArgs e) => ((EventHandler)GetValue(ExpanderItemClickProperty))?.Invoke(sender, e);

        private void UccExpanderMenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            Background = Brushes.AliceBlue;
        }

        private void UccExpanderMenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            Background = InitalBackground;
        }
    }
}
