using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Controls;
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

        ExpDisMode _displayMode = ExpDisMode.Hidden;

        public ExpDisMode DisplayMode
        {
            set
            {
                if (_displayMode != value)
                {
                    _displayMode = value;
                    OnPropertyChanged("TagBrush");
                    OnPropertyChanged("Background");
                    OnPropertyChanged("Highlight");
                }
            }
            get { return _displayMode; }
        }

        public Brush TagBrush
        {
            get
            {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyNameBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyNameBrushNormal : ThemeManager.WhiteKeyNameBrushNormal;
            }
        }
        public Brush Background
        {
            get
            {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyBrushNormal : ThemeManager.WhiteKeyBrushNormal;
            }
        }
        public Brush Highlight
        {
            get
            {
                return DisplayMode == ExpDisMode.Visible ? Brushes.Black :
                    DisplayMode == ExpDisMode.Shadow ? Brushes.Black : Brushes.Black;
            }
        }

        public ExpComboBoxViewModel() { this.Subscribe(DocManager.Inst); }

        public void CreateBindings(ExpComboBox box)
        {
            box.DataContext = this;
            box.SetBinding(ExpComboBox.ItemsSourceProperty, new Binding("Keys") { Source = this });
            box.SetBinding(ExpComboBox.SelectedIndexProperty, new Binding("SelectedIndex") { Source = this, Mode = BindingMode.TwoWay });
            box.SetBinding(ExpComboBox.TagBrushProperty, new Binding("TagBrush") { Source = this });
            box.SetBinding(ExpComboBox.BackgroundProperty, new Binding("Background") { Source = this });
            box.SetBinding(ExpComboBox.HighlightProperty, new Binding("Highlight") { Source = this });
            box.Click += box_Click;
            box.SelectionChanged += box_SelectionChanged;
        }

        void box_Click(object sender, EventArgs e)
        {
            if (DisplayMode != ExpDisMode.Visible)
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index, true));
        }

        void box_SelectionChanged(object sender, EventArgs e)
        {
            if (DisplayMode != ExpDisMode.Visible)
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index, true));
            else
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index, false));
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
            DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(_keys[Index % _keys.Count], this.Index, true));
            OnPropertyChanged("Keys");
        }

        private void OnSelectExp(SelectExpressionNotification cmd)
        {
            if (cmd.SelectorIndex == this.Index)
            {
                if (Keys[SelectedIndex] != cmd.ExpKey)
                {
                    SelectedIndex = Keys.IndexOf(cmd.ExpKey);
                }
                DisplayMode = ExpDisMode.Visible;
            }
            else if (cmd.UpdateShadow)
            {
                DisplayMode = DisplayMode == ExpDisMode.Visible ? ExpDisMode.Shadow : ExpDisMode.Hidden;
            }
        }

        # endregion
    }
}
