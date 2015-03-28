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

using OpenUtau.Core;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for NoteControl.xaml
    /// </summary>
    public partial class NoteControl : UserControl
    {
        public Note note;
        
        public NoteControl()
        {
            InitializeComponent();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualHeight < 10) lyricText.Visibility = System.Windows.Visibility.Hidden;
            else lyricText.Visibility = System.Windows.Visibility.Visible;
        }

        public void SetSelected()
        {
            bodyRectangle.Fill = Brushes.Red;
        }

        public void SetUnselected()
        {
            bodyRectangle.Fill = Brushes.Gray;
        }


    }
}
