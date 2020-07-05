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
    /// Interaction logic for wTooltip.xaml
    /// </summary>
    public partial class wTooltip : Window
    {
        public wTooltip(string text)
        {
            InitializeComponent();
            lblText.Content =  new TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap }; ;
        }
    }
}
