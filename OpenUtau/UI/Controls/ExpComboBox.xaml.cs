using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI.Controls {
    /// <summary>
    /// Interaction logic for ExpComboBox.xaml
    /// </summary>
    public partial class ExpComboBox : UserControl {
        public event EventHandler Click;
        public event EventHandler SelectionChanged;

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(ExpComboBox), new PropertyMetadata(0));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ExpComboBox));
        public static readonly DependencyProperty TagBrushProperty = DependencyProperty.Register(nameof(TagBrush), typeof(Brush), typeof(ExpComboBox), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(nameof(Highlight), typeof(Brush), typeof(ExpComboBox), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExpComboBox), new PropertyMetadata(""));

        public int SelectedIndex {
            set => SetValue(SelectedIndexProperty, value);
            get => (int)GetValue(SelectedIndexProperty);
        }
        public IEnumerable ItemsSource {
            set => SetValue(ItemsSourceProperty, value);
            get => (IEnumerable)GetValue(ItemsSourceProperty);
        }
        public Brush TagBrush {
            set => SetValue(TagBrushProperty, value);
            get => (Brush)GetValue(TagBrushProperty);
        }
        public Brush Highlight {
            set => SetValue(HighlightProperty, value);
            get => (Brush)GetValue(HighlightProperty);
        }
        public string Text {
            set => SetValue(TextProperty, value);
            get => (string)GetValue(TextProperty);
        }

        public ExpComboBox() {
            InitializeComponent();
        }

        private void mainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            EventHandler handler = Click;
            if (handler != null) handler(this, e);
        }

        private void dropList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != 1) {
                return;
            }
            string name = ((UExpressionDescriptor)e.AddedItems[0]).name;
            string abbr = ((UExpressionDescriptor)e.AddedItems[0]).abbr;
            Text = abbr.ToUpper();
            EventHandler handler = SelectionChanged;
            if (handler != null) {
                handler(this, e);
            }
        }
    }
}
