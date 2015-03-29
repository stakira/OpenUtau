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
using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for NoteControl.xaml
    /// </summary>
    public partial class NoteControl : UserControl
    {
        public Note note;

        int _channel = 0;

        public int Channel
        {
            set
            {
                _channel = value;
            }
            get
            {
                return _channel;
            }
        }
        
        public NoteControl()
        {
            InitializeComponent();
            SetUnselected();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualHeight < 10 ||
                ActualWidth < lyricText.ActualWidth + 5)
                lyricText.Visibility = System.Windows.Visibility.Hidden;
            else lyricText.Visibility = System.Windows.Visibility.Visible;
        }

        public void SetUnselected()
        {
            bodyRectangle.Fill = ThemeManager.NoteFillBrushes[_channel];
            bodyRectangle.Stroke = ThemeManager.NoteStrokeBrushes[_channel];
        }

        public void SetSelected()
        {
            bodyRectangle.Fill = ThemeManager.NoteFillActiveBrushes[_channel];
            bodyRectangle.Stroke = ThemeManager.NoteStrokeActiveBrushes[_channel];
        }
    }
}
