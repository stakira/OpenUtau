using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.App.Controls {
    public partial class NotePropertiesControl : UserControl, ICmdSubscriber {
        private readonly NotePropertiesViewModel ViewModel;

        public NotePropertiesControl() {
            InitializeComponent();
            DataContext = ViewModel = new NotePropertiesViewModel();

            DocManager.Inst.AddSubscriber(this);
        }

        private void LoadPart(UPart? part) {
            if (NotePropertiesViewModel.PanelControlPressed) {
                NotePropertiesViewModel.PanelControlPressed = false;
                DocManager.Inst.EndUndoGroup();
            }
            NotePropertiesViewModel.NoteLoading = true;

            ViewModel.LoadPart(part);
            ExpressionsPanel.Children.Clear();
            foreach (NotePropertyExpViewModel expVM in ViewModel.Expressions) {
                var control = new NotePropertyExpression() { DataContext = expVM };
                ExpressionsPanel.Children.Add(control);
            }

            NotePropertiesViewModel.NoteLoading = false;
        }

        void OnGotFocus(object sender, GotFocusEventArgs e) {
            Log.Information("Note property panel got focus");
            DocManager.Inst.StartUndoGroup();
            NotePropertiesViewModel.PanelControlPressed = true;
        }
        void OnLostFocus(object sender, RoutedEventArgs e) {
            Log.Information("Note property panel lost focus");
            NotePropertiesViewModel.PanelControlPressed = false;
            DocManager.Inst.EndUndoGroup();
        }

        void VibratoEnableClicked(object sender, RoutedEventArgs e) {
            ViewModel.SetVibratoEnable();
        }

        void OnSavePortamentoPreset(object sender, RoutedEventArgs e) {
            if (VisualRoot is Window window) {
                var dialog = new TypeInDialog() {
                    Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                    onFinish = name => ViewModel.SavePortamentoPreset(name),
                };
                dialog.ShowDialog(window);
            }
        }

        void OnRemovePortamentoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedPortamentoPreset();
        }

        void OnSaveVibratoPreset(object sender, RoutedEventArgs e) {
            if (VisualRoot is Window window) {
                var dialog = new TypeInDialog() {
                    Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                    onFinish = name => ViewModel.SaveVibratoPreset(name),
                };
                dialog.ShowDialog(window);
            }
        }

        void OnRemoveVibratoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedVibratoPreset();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification notif) {
                if (cmd is LoadPartNotification) {
                    LoadPart(notif.part);
                } else if (cmd is LoadProjectNotification) {
                    LoadPart(null);
                } else if (cmd is SingersRefreshedNotification) {
                    LoadPart(notif.part);
                }
            } else if (cmd is TrackCommand) {
                if (cmd is RemoveTrackCommand removeTrack) {
                    if (ViewModel.Part != null && removeTrack.removedParts.Contains(ViewModel.Part)) {
                        LoadPart(null);
                    }
                } else if (cmd is TrackChangeSingerCommand trackChangeSinger) {
                    if (ViewModel.Part != null && trackChangeSinger.track.TrackNo == ViewModel.Part.trackNo) {
                        // LoadPart(ViewModel.Part); can't load crl
                    }
                }
            }
        }
    }
}
