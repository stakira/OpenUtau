using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Data;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.UI.Controls;
using ReactiveUI;

namespace OpenUtau.UI.Models {
    class ExpComboBoxViewModel : ReactiveObject, ICmdSubscriber {
        public int Index;

        public int SelectedIndex {
            get => _selectedIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
        }
        public ExpDisMode DisplayMode {
            get => _displayMode;
            set => this.RaiseAndSetIfChanged(ref _displayMode, value);
        }
        public ObservableCollection<UExpressionDescriptor> Descriptors => _descriptors;
        public Brush TagBrush => _tagBrush.Value;
        public Brush Background => _background.Value;

        int _selectedIndex;
        ExpDisMode _displayMode = ExpDisMode.Hidden;
        ObservableCollection<UExpressionDescriptor> _descriptors = new ObservableCollection<UExpressionDescriptor>();
        ObservableAsPropertyHelper<Brush> _tagBrush;
        ObservableAsPropertyHelper<Brush> _background;

        public ExpComboBoxViewModel() {
            DocManager.Inst.AddSubscriber(this);
            this.WhenAnyValue(x => x.DisplayMode)
                .Select(mode =>
                    mode == ExpDisMode.Visible ? ThemeManager.ActiveExpNameBrush :
                    mode == ExpDisMode.Shadow ? ThemeManager.ShadowExpNameBrush : ThemeManager.NormalExpNameBrush)
                .ToProperty(this, x => x.TagBrush, out _tagBrush);
            this.WhenAnyValue(x => x.DisplayMode)
                .Select(mode =>
                    mode == ExpDisMode.Visible ? (Brush)ThemeManager.ActiveExpBrush :
                    mode == ExpDisMode.Shadow ? (Brush)ThemeManager.ShadowExpBrush : Brushes.Transparent)
                .ToProperty(this, x => x.Background, out _background);
        }

        public void CreateBindings(ExpComboBox box) {
            box.DataContext = this;
            box.SetBinding(ExpComboBox.ItemsSourceProperty, new Binding(nameof(Descriptors)) { Source = this,/* Converter = new ExpressionDescriptorNameConverter()*/ });
            box.SetBinding(ExpComboBox.SelectedIndexProperty, new Binding(nameof(SelectedIndex)) { Source = this, Mode = BindingMode.TwoWay });
            box.SetBinding(ExpComboBox.TagBrushProperty, new Binding(nameof(TagBrush)) { Source = this });
            box.SetBinding(ExpComboBox.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
            box.Click += box_Click;
            box.SelectionChanged += box_SelectionChanged;
        }

        void box_Click(object sender, EventArgs e) {
            if (DisplayMode != ExpDisMode.Visible) {
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptors[SelectedIndex].abbr, Index, true));
            }
        }

        void box_SelectionChanged(object sender, EventArgs e) {
            DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptors[SelectedIndex].abbr, Index, DisplayMode != ExpDisMode.Visible));
        }

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ChangeExpressionListNotification ||
                cmd is LoadProjectNotification ||
                cmd is LoadPartNotification ||
                cmd is ConfigureExpressionsCommand) {
                OnListChange();
            } else if (cmd is SelectExpressionNotification) {
                OnSelectExp((SelectExpressionNotification)cmd);
            }
        }

        # endregion

        # region Cmd Handling

        private void OnListChange() {
            var selectedIndex = SelectedIndex;
            Descriptors.Clear();
            DocManager.Inst.Project.expressions.Values.ToList().ForEach(Descriptors.Add);
            if (selectedIndex >= _descriptors.Count) {
                selectedIndex = Index;
            }
            SelectedIndex = selectedIndex;
        }

        private void OnSelectExp(SelectExpressionNotification cmd) {
            if (Descriptors.Count == 0) {
                return;
            }
            if (cmd.SelectorIndex == Index) {
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
