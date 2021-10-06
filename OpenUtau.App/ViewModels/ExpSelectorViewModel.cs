using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using OpenUtau.App.Controls;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExpSelectorViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public int Index { get; set; }
        [Reactive] public int SelectedIndex { get; set; }
        [Reactive] public ExpDisMode DisplayMode { get; set; }
        [Reactive] public UExpressionDescriptor? Descriptor { get; set; }
        public ObservableCollection<UExpressionDescriptor> Descriptors => descriptors;
        public string Header => header.Value;
        public IBrush TagBrush => tagBrush.Value;
        public IBrush Background => background.Value;

        ObservableCollection<UExpressionDescriptor> descriptors = new ObservableCollection<UExpressionDescriptor>();
        ObservableAsPropertyHelper<string> header;
        ObservableAsPropertyHelper<IBrush> tagBrush;
        ObservableAsPropertyHelper<IBrush> background;

        public ExpSelectorViewModel() {
            DocManager.Inst.AddSubscriber(this);
            this.WhenAnyValue(x => x.DisplayMode)
                .Select(mode =>
                    mode == ExpDisMode.Visible ? Brushes.White :
                    mode == ExpDisMode.Shadow ? ThemeManager.AccentBrush3! : ThemeManager.AccentBrush3!)
                .ToProperty(this, x => x.TagBrush, out tagBrush);
            this.WhenAnyValue(x => x.DisplayMode)
                .Select(mode =>
                    mode == ExpDisMode.Visible ? ThemeManager.AccentBrush3! :
                    mode == ExpDisMode.Shadow ? ThemeManager.AccentBrush3Semi! : Brushes.Transparent)
                .ToProperty(this, x => x.Background, out background);
            this.WhenAnyValue(x => x.Descriptor)
                .Select(descriptor => descriptor == null ? string.Empty : descriptor.abbr.ToUpperInvariant())
                .ToProperty(this, x => x.Header, out header);
            this.WhenAnyValue(x => x.Descriptor)
                .Subscribe(SelectionChanged);
            this.WhenAnyValue(x => x.Index, x => x.Descriptors)
                .Subscribe(tuple => {
                    if (tuple.Item2 != null && tuple.Item2.Count > tuple.Item1) {
                        Descriptor = tuple.Item2[tuple.Item1];
                    }
                });
            OnListChange();
        }

        public void OnSelected() {
            if (DisplayMode != ExpDisMode.Visible && Descriptor != null) {
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(Descriptor.abbr, Index, true));
            }
        }

        void SelectionChanged(UExpressionDescriptor? descriptor) {
            if (descriptor != null) {
                DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(descriptor.abbr, Index, DisplayMode != ExpDisMode.Visible));
            }
        }

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

        private void OnListChange() {
            var selectedIndex = SelectedIndex;
            Descriptors.Clear();
            DocManager.Inst.Project.expressions.Values.ToList().ForEach(Descriptors.Add);
            if (selectedIndex >= descriptors.Count) {
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
    }
}
