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

namespace OpenUtau.comm
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class BorderlessWindow : Window
    {
        private Rect _restoreLocation;
        private Thickness _maxizedMainBorderMargin;
        private Thickness _normalMainBorderMargin;

        public BorderlessWindow()
        {
            InitializeComponent();
            _maxizedMainBorderMargin = new Thickness(0, 0, 0, 0);
            _normalMainBorderMargin = mainBorder.Margin;

        }


        private void MaximizeWindow()
        {
            // Resize window
            _restoreLocation = new Rect { Width = Width, Height = Height, X = Left, Y = Top };
            System.Windows.Forms.Screen currentScreen;
            currentScreen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            Height = currentScreen.WorkingArea.Height;
            Width = currentScreen.WorkingArea.Width;
            Left = currentScreen.WorkingArea.X;
            Top = currentScreen.WorkingArea.Y;
            // Remove shadow
            canvasBorder.Margin = _maxizedMainBorderMargin;
            canvasBorderDropShadow.Opacity = 0;
        }

        private void Restore()
        {
            // Resize window
            Height = _restoreLocation.Height;
            Width = _restoreLocation.Width;
            Left = _restoreLocation.X;
            Top = _restoreLocation.Y;
            // Recover shadow
            canvasBorder.Margin = _normalMainBorderMargin;
            canvasBorderDropShadow.Opacity = 0;
        }
    }
}
