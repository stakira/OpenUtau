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

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for NavigateDrag.xaml
    /// </summary>
    public partial class NavigateDrag : UserControl
    {
        Brush PathActiveBrush { set; get; }

        public event EventHandler Drag;

        public NavigateDrag()
        {
            InitializeComponent();
        }

        private void NavigateDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void NavigateDrag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void NavigateDrag_MouseMove(object sender, MouseEventArgs e)
        {

        }
    }
}
