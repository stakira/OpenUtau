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
using System.Runtime.InteropServices;

namespace OpenUtau.comm
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class BorderlessWindow : Window
    {
        private Rect _restoreLocation;
        private bool _maximized;
        private Thickness _maxizedCanvasMargin;
        private Thickness _normalCanvasMargin;

        public BorderlessWindow()
        {
            InitializeComponent();
            _maximized = false;
            _maxizedCanvasMargin = new Thickness(0, 0, 0, 0);
            _normalCanvasMargin = canvasBorder.Margin;
        }

        private void MaximizeWindow()
        {
            // Resize window
            _restoreLocation = new Rect { Width = Width, Height = Height, X = Left, Y = Top };
            System.Windows.Forms.Screen currentScreen;
            currentScreen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            Top = currentScreen.WorkingArea.Y;
            Left = currentScreen.WorkingArea.X;
            Width = currentScreen.WorkingArea.Width;
            Height = currentScreen.WorkingArea.Height;
            // Remove shadow
            canvasBorder.Margin = _maxizedCanvasMargin;
            canvasBorderDropShadow.Opacity = 0;
        }

        private void Restore()
        {
            // Resize window
            Top = _restoreLocation.Y;
            Left = _restoreLocation.X;
            Width = _restoreLocation.Width;
            Height = _restoreLocation.Height;
            // Recover shadow
            canvasBorder.Margin = _normalCanvasMargin;
            canvasBorderDropShadow.Opacity = 0.75;
        }

        private void minButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void maxButton_Click(object sender, RoutedEventArgs e)
        {
            if (_maximized) {
                _maximized = false;
                Restore();
            }
            else {
                _maximized = true;
                MaximizeWindow();
            }
                
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void titleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            canvasBorderDropShadow.BlurRadius = 30;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            canvasBorderDropShadow.BlurRadius = 20;
        }
    }
}
