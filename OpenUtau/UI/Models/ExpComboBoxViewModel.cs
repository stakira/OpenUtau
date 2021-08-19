using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Data;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.UI.Controls;
using System.Globalization;

namespace OpenUtau.UI.Models {
    [ValueConversion(typeof(ObservableCollection<UExpressionDescriptor>), typeof(ObservableCollection<string>))]
    public class ExpressionDescriptorNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return new ObservableCollection<string>(((IEnumerable<UExpressionDescriptor>)value).Select(d => d.name));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    class ExpComboBoxViewModel : INotifyPropertyChanged, ICmdSubscriber {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public int Index;

        int _selectedIndex;
        public int SelectedIndex {
            set {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
            get {
                return _selectedIndex;
            }
        }

        ObservableCollection<UExpressionDescriptor> _descriptors = new ObservableCollection<UExpressionDescriptor>();
        public ObservableCollection<UExpressionDescriptor> Descriptors { get { return _descriptors; } }

        ExpDisMode _displayMode = ExpDisMode.Hidden;

        public ExpDisMode DisplayMode {
            set {
                if (_displayMode != value) {
                    _displayMode = value;
                    OnPropertyChanged(nameof(TagBrush));
                    OnPropertyChanged(nameof(Background));
                    OnPropertyChanged(nameof(Highlight));
                }
            }
            get { return _displayMode; }
        }

        public Brush TagBrush {
            get {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyNameBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyNameBrushNormal : ThemeManager.WhiteKeyNameBrushNormal;
            }
        }
        public Brush Background {
            get {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyBrushNormal : ThemeManager.WhiteKeyBrushNormal;
            }
        }
        public Brush Highlight {
            get {
                return DisplayMode == ExpDisMode.Visible ? Brushes.Black :
                    DisplayMode == ExpDisMode.Shadow ? Brushes.Black : Brushes.Black;
            }
        }

        public ExpComboBoxViewModel() {
            DocManager.Inst.AddSubscriber(this);
        }

        public void CreateBindings(ExpComboBox box) {
            box.DataContext = this;
            box.SetBinding(ExpComboBox.ItemsSourceProperty, new Binding(nameof(Descriptors)) { Source = this, Converter = new ExpressionDescriptorNameConverter() });
            box.SetBinding(ExpComboBox.SelectedIndexProperty, new Binding(nameof(SelectedIndex)) { Source = this, Mode = BindingMode.TwoWay });
            box.SetBinding(ExpComboBox.TagBrushProperty, new Binding(nameof(TagBrush)) { Source = this });
            box.SetBinding(ExpComboBox.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
            box.SetBinding(ExpComboBox.HighlightProperty, new Binding(nameof(Highlight)) { Source = this });
            box.Click += box_Click;
            box.SelectionChanged += box_SelectionChanged;
        }

        void box_Click(object sender, EventArgs e) {
            if (DisplayMode != ExpDisMode.Visible)
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptors[SelectedIndex].abbr, this.Index, true));
        }

        void box_SelectionChanged(object sender, EventArgs e) {
            if (DisplayMode != ExpDisMode.Visible)
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptors[SelectedIndex].abbr, this.Index, true));
            else
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptors[SelectedIndex].abbr, this.Index, false));
        }

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ChangeExpressionListNotification || cmd is LoadProjectNotification) {
                OnListChange();
            } else if (cmd is LoadPartNotification) {
                if (_descriptors.Count == 0) OnListChange();
            } else if (cmd is SelectExpressionNotification) {
                OnSelectExp((SelectExpressionNotification)cmd);
            }
        }

        # endregion

        # region Cmd Handling

        private void OnListChange() {
            _descriptors = new ObservableCollection<UExpressionDescriptor>(DocManager.Inst.Project.expressions.Values);
            if (_descriptors.Count > 0) SelectedIndex = Index % _descriptors.Count;
            OnPropertyChanged(nameof(Descriptors));
        }

        private void OnSelectExp(SelectExpressionNotification cmd) {
            if (Descriptors.Count == 0) return;
            if (cmd.SelectorIndex == this.Index) {
                if (Descriptors[SelectedIndex].abbr != cmd.ExpKey) {
                    SelectedIndex = Descriptors.IndexOf(Descriptors.First(d => d.abbr == cmd.ExpKey));
                }
                DisplayMode = ExpDisMode.Visible;
            } else if (cmd.UpdateShadow) {
                DisplayMode = DisplayMode == ExpDisMode.Visible ? ExpDisMode.Shadow : ExpDisMode.Hidden;
            }
        }

        # endregion
    }
}
