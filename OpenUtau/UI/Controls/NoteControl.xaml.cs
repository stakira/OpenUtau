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
using OpenUtau.Core.USTx;
using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for NoteControl.xaml
    /// </summary>
    public partial class NoteControl : UserControl
    {
        public UNote Note;

        int _channel = 0;
        bool _error = false;
        bool _selected = false;
        string _lyric = "a";

        Rectangle lyricBoxBorder;
        TextBox lyricBox;

        public int Channel
        {
            set { if (_channel != value) { _channel = value; UpdateColors(); } }
            get { return _channel; }
        }

        public bool Error
        {
            set { if (_error != value) { _error = value; UpdateColors(); } }
            get { return _error; }
        }

        public bool Selected
        {
            set { if (_selected != value) { _selected = value; UpdateColors(); } }
            get { return _selected; }
        }

        public string Lyric
        {
            set { _lyric = value; lyricText.Text = value; AbbreLyricText.Text = value[0] + ".."; }
            get { return _lyric; }
        }
        
        public NoteControl()
        {
            InitializeComponent();
            UpdateColors();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            bodyRectangle.Width = this.ActualWidth;
            bodyRectangle.Height = this.ActualHeight;

            if (ActualHeight < 10)
            {
                lyricText.Visibility = System.Windows.Visibility.Hidden;
                AbbreLyricText.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (ActualWidth < lyricText.ActualWidth + 5)
            {
                lyricText.Visibility = System.Windows.Visibility.Hidden;
                Canvas.SetTop(AbbreLyricText, (this.ActualHeight - AbbreLyricText.ActualHeight) / 2);
                AbbreLyricText.Visibility = System.Windows.Visibility.Visible;
                AbbreLyricText.Width = Math.Max(0, ActualWidth - 5);
            }
            else
            {
                Canvas.SetTop(lyricText, (this.ActualHeight - lyricText.ActualHeight) / 2);
                lyricText.Visibility = System.Windows.Visibility.Visible;
                AbbreLyricText.Visibility = System.Windows.Visibility.Hidden;
            }

            if (lyricBox != null) lyricBox.Height = this.ActualHeight - 2;
            if (lyricBoxBorder != null) lyricBoxBorder.Height = this.ActualHeight;
        }

        private void UpdateColors()
        {
            if (_selected && _error)
            {
                bodyRectangle.Fill = ThemeManager.NoteFillSelectedErrorBrushes;
            }
            else if (_selected)
            {
                bodyRectangle.Fill = ThemeManager.NoteFillSelectedBrush;
            }
            else if (_error)
            {
                bodyRectangle.Fill = ThemeManager.NoteFillErrorBrushes[_channel];
            }
            else
            {
                bodyRectangle.Fill = ThemeManager.NoteFillBrushes[_channel];
            }

            if (_error)
            {
                lyricText.Foreground = Brushes.Red;
                bodyRectangle.Stroke = ThemeManager.NoteStrokeErrorBrush;
                bodyRectangle.StrokeThickness = 1;
            }
            else
            {
                lyricText.Foreground = Brushes.White;
                bodyRectangle.StrokeThickness = 0;
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lyricBoxBorder = new Rectangle()
            {
                Width = 66,
                Height = bodyCanvas.ActualHeight,
                Stroke = bodyRectangle.Fill,
                StrokeThickness = 1
            };
            bodyCanvas.Children.Add(lyricBoxBorder);
            Canvas.SetLeft(lyricBoxBorder, 1);
            lyricBox = new TextBox()
            {
                Width = 64,
                Height = bodyCanvas.ActualHeight - 2,
                SelectedText = _lyric,
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                BorderThickness = new Thickness(0)
            };
            lyricBox.Loaded += lyricBox_Loaded;
            lyricBox.LostFocus += lyricBox_LostFocus;
            bodyCanvas.Children.Add(lyricBox);
            Canvas.SetLeft(lyricBox, 1);
            Canvas.SetTop(lyricBox, 1);
            Canvas.SetZIndex(this, UIConstants.NoteWithLyricBoxZIndex);
        }

        void lyricBox_Loaded(object sender, RoutedEventArgs e)
        {
            lyricBox.Focus();
        }

        void lyricBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).Text.Trim() != "") Lyric = ((TextBox)sender).Text.Trim();
            bodyCanvas.Children.Remove((TextBox)lyricBox);
            lyricBox.Loaded -= lyricBox_Loaded;
            lyricBox.LostFocus -= lyricBox_LostFocus;
            lyricBox = null;
            bodyCanvas.Children.Remove((Rectangle)lyricBoxBorder);
            lyricBoxBorder = null;
            Canvas.SetZIndex(this, UIConstants.NoteZIndex);
        }

        public bool IsLyricBoxActive()
        {
            return lyricBox != null;
        }
    }
}
