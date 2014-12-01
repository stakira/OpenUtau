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
using System.Windows.Shell;
//using System.Runtime.InteropServices;

namespace OpenUtau.comm
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class BorderlessWindow : Window
    {
        private Rect _restoreLocation;
        private bool _maximized;
        WindowChrome _chrome;

        public BorderlessWindow()
        {
            InitializeComponent();
            _chrome = new WindowChrome();
            WindowChrome.SetWindowChrome(this, _chrome);
            _chrome.GlassFrameThickness = new Thickness(1);
            _chrome.CornerRadius = new CornerRadius(0);
            _chrome.CaptionHeight = 0;
            _maximized = false;
        }

        private void MaximizeWindow()
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            _maximized = true;
            // Resize window
            _restoreLocation = new Rect { Width = Width, Height = Height, X = Left, Y = Top };
            System.Windows.Forms.Screen currentScreen;
            currentScreen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            Top = currentScreen.WorkingArea.Y;
            Left = currentScreen.WorkingArea.X;
            Width = currentScreen.WorkingArea.Width;
            Height = currentScreen.WorkingArea.Height;
        }

        private void Restore()
        {
            this.ResizeMode = System.Windows.ResizeMode.CanResize;
            _maximized = false;
            // Resize window
            Top = _restoreLocation.Y;
            Left = _restoreLocation.X;
            Width = _restoreLocation.Width;
            Height = _restoreLocation.Height;
        }

        private void minButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void maxButton_Click(object sender, RoutedEventArgs e)
        {
            if (_maximized) {
                Restore();
            }
            else {
                MaximizeWindow();
            }
                
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void dragMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (_maximized)
                {
                    _maximized = false;
                    Restore();
                }
                else
                {
                    _maximized = true;
                    MaximizeWindow();
                }
            }
            else if (!_maximized)
            {
                DragMove();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.canvasBorder.BorderBrush = Brushes.White;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_maximized) this.canvasBorder.BorderBrush = Brushes.Gray;
        }
    }
}
