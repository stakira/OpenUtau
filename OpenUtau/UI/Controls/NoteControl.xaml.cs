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
        bool _error = false;
        bool _selected = false;
        string _lyric = "a";

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
            if (ActualHeight < 10)
            {
                lyricText.Visibility = System.Windows.Visibility.Hidden;
                AbbreLyricText.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (ActualWidth < lyricText.ActualWidth + 5)
            {
                lyricText.Visibility = System.Windows.Visibility.Hidden;
                AbbreLyricText.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                lyricText.Visibility = System.Windows.Visibility.Visible;
                AbbreLyricText.Visibility = System.Windows.Visibility.Hidden;
            }

            if (lyricBox != null) lyricBox.Height = this.ActualHeight;
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
            System.Diagnostics.Debug.Print("Edit lyric " + _lyric);
            lyricBox = new TextBox() { Width = 64, Height = bodyCanvas.ActualHeight, SelectedText = _lyric, VerticalContentAlignment = System.Windows.VerticalAlignment.Center };
            lyricBox.Loaded += lyricBox_Loaded;
            lyricBox.LostFocus += lyricBox_LostFocus;
            bodyCanvas.Children.Add(lyricBox);
            Canvas.SetZIndex(this, UIConstants.NoteWithLyricBoxZIndex);
        }

        void lyricBox_Loaded(object sender, RoutedEventArgs e)
        {
            lyricBox.Focus();
        }

        void lyricBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).Text.Trim() != "") Lyric = ((TextBox)sender).Text.Trim();
            bodyCanvas.Children.Remove((TextBox)sender);
            lyricBox = null;
            Canvas.SetZIndex(this, UIConstants.NoteZIndex);
        }
    }
}
