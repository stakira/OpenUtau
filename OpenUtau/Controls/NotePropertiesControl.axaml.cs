using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    public partial class NotePropertiesControl : UserControl, ICmdSubscriber {
        private readonly NotePropertiesViewModel ViewModel;

        public NotePropertiesControl() {
            InitializeComponent();
            DataContext = ViewModel = new NotePropertiesViewModel();

            DocManager.Inst.AddSubscriber(this);
        }

        private void LoadPart(UPart? part) {
            ViewModel.LoadPart(part);
            ExpressionsPanel.Children.Clear();
            foreach (NotePropertyExpViewModel expVM in ViewModel.Expressions) {
                var control = new NotePropertyExpression() { DataContext = expVM };
                ExpressionsPanel.Children.Add(control);
            }
        }

        void OnSavePortamentoPreset(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                onFinish = name => ViewModel.SavePortamentoPreset(name),
            };
            //dialog.ShowDialog(this);
        }

        void OnRemovePortamentoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedPortamentoPreset();
        }

        void OnSaveVibratoPreset(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                onFinish = name => ViewModel.SaveVibratoPreset(name),
            };
            //dialog.ShowDialog(this);
        }

        void OnRemoveVibratoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedVibratoPreset();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    //OnFinish(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    //OnCancel(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadPartNotification loadPart) {
                LoadPart(loadPart.part);
            }
        }
    }
}
