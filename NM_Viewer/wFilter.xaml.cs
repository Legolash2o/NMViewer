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
using System.Windows.Shapes;

namespace NM_Viewer
{
    /// <summary>
    /// Interaction logic for wFilter.xaml
    /// </summary>
    public partial class wFilter : Window
    {
        public wFilter()
        {
            InitializeComponent();
        }

        public bool Tiplocs = true;

        public bool Elocs = false;

        public bool Signals = false;

        private void BtnGo_OnClick(object sender, RoutedEventArgs e)
        {
            Tiplocs = (bool) chkTiploc.IsChecked;
            Elocs = (bool)chkELOCS.IsChecked;
            Signals = (bool)chkSignals.IsChecked;
            this.Close();
        }
    }
}
