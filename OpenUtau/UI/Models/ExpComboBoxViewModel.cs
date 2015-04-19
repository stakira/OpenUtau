using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Data;

using OpenUtau.Core.USTx;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    public class ExpComboBoxViewModel : INotifyPropertyChanged, ICmdSubscriber
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public int Index;

        int _selectedIndex;
        public int SelectedIndex { set { _selectedIndex = value; OnPropertyChanged("SelectedIndex"); } get { return _selectedIndex; } }

        ObservableCollection<string> _keys = new ObservableCollection<string>();
        public ObservableCollection<string> Keys { get { return _keys; } }

        bool _isChecked = false;
        public bool IsChecked
        {
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged("Foreground");
                    OnPropertyChanged("Background");
                    OnPropertyChanged("Highlight");
                }
            }
            get { return _isChecked; }
        }

        public Brush Foreground { get { return IsChecked ? ThemeManager.BlackKeyNameBrushNormal : ThemeManager.WhiteKeyNameBrushNormal; } }
        public Brush Background { get { return IsChecked ? ThemeManager.BlackKeyBrushNormal : ThemeManager.WhiteKeyBrushNormal; } }
        public Brush Highlight { get { return IsChecked ? Brushes.Black : Brushes.Black; } }

        public ExpComboBoxViewModel() { this.Subscribe(DocManager.Inst); }

        public void CreateBindings(ExpComboBox box)
        {
            box.DataContext = this;
            box.SetBinding(ExpComboBox.ItemsSourceProperty, new Binding("Keys") { Source = this });
            box.SetBinding(ExpComboBox.SelectedIndexProperty, new Binding("SelectedIndex") { Source = this });
            box.SetBinding(ExpComboBox.ForegroundProperty, new Binding("Foreground") { Source = this });
            box.SetBinding(ExpComboBox.BackgroundProperty, new Binding("Background") { Source = this });
            box.SetBinding(ExpComboBox.HighlightProperty, new Binding("Highlight") { Source = this });
        }

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is ChangeExpressionListNotification || cmd is LoadProjectNotification) OnListChange();
            else if (cmd is LoadPartNotification) { if (_keys.Count == 0) OnListChange(); }
            else if (cmd is SelectExpressionNotification) OnSelectExp((SelectExpressionNotification)cmd);
        }

        # endregion

        # region Cmd Handling

        private void OnListChange()
        {
            _keys = new ObservableCollection<string>(DocManager.Inst.Project.ExpressionTable.Keys);
            if (_keys.Count == 0) return;
            DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(_keys[Index % _keys.Count], this));
            OnPropertyChanged("Keys");
        }

        private void OnSelectExp(SelectExpressionNotification cmd)
        {
            if (Keys.Count > SelectedIndex && cmd.VM == this)
            {
                if (Keys[SelectedIndex] != cmd.ExpKey)
                {
                    SelectedIndex = Keys.IndexOf(cmd.ExpKey);
                    // change visual
                }
                // switch visual
            }
        }

        # endregion
    }
}
