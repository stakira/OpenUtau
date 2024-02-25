using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using Serilog;
using SharpCompress;

namespace OpenUtau.App.Controls {
    public partial class NotePropertiesControl : UserControl, ICmdSubscriber {
        private readonly NotePropertiesViewModel ViewModel;

        public NotePropertiesControl() {
            InitializeComponent();
            DataContext = ViewModel = new NotePropertiesViewModel();

            this.GetLogicalDescendants().OfType<TextBox>().ForEach(box => {
                box.AddHandler(GotFocusEvent, OnTextBoxGotFocus);
                box.AddHandler(LostFocusEvent, OnTextBoxLostFocus);
            });
            this.GetLogicalDescendants().OfType<Slider>().ForEach(slider => {
                slider.AddHandler(PointerPressedEvent, SliderPointerPressed, RoutingStrategies.Tunnel);
                slider.AddHandler(PointerReleasedEvent, SliderPointerReleased, RoutingStrategies.Tunnel);
                slider.AddHandler(PointerMovedEvent, SliderPointerMoved, RoutingStrategies.Tunnel);
            });
          
            MessageBus.Current.Listen<PianorollRefreshEvent>()
                .Subscribe(e => {
                    if(e.refreshItem == "Part") {
                        LoadPart(ViewModel.Part);
                    }
                });

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

        private string textBoxValue = string.Empty;
        void OnTextBoxGotFocus(object? sender, GotFocusEventArgs args) {
            Log.Debug("Note property textbox got focus");
            if(sender is TextBox text) {
                textBoxValue = text.Text ?? string.Empty;
            }
        }
        void OnTextBoxLostFocus(object? sender, RoutedEventArgs args) {
            Log.Debug("Note property textbox lost focus");
            if (sender is TextBox textBox && textBoxValue != textBox.Text && textBox.Tag is string tag && !string.IsNullOrEmpty(tag)) {
                DocManager.Inst.StartUndoGroup();
                NotePropertiesViewModel.PanelControlPressed = true;
                ViewModel.SetNoteParams(tag, textBox.Text);
                NotePropertiesViewModel.PanelControlPressed = false;
                DocManager.Inst.EndUndoGroup();
            }
        }

        void SliderPointerPressed(object? sender, PointerPressedEventArgs args) {
            Log.Debug("Slider pressed");
            if (sender is Control control) {
                var point = args.GetCurrentPoint(control);
                if (point.Properties.IsLeftButtonPressed) {
                    DocManager.Inst.StartUndoGroup();
                    NotePropertiesViewModel.PanelControlPressed = true;
                } else if (point.Properties.IsRightButtonPressed) {
                    if (control.Tag is string tag && !string.IsNullOrEmpty(tag)) {
                        DocManager.Inst.StartUndoGroup();
                        NotePropertiesViewModel.PanelControlPressed = true;
                        ViewModel.SetNoteParams(tag, null);
                        NotePropertiesViewModel.PanelControlPressed = false;
                        DocManager.Inst.EndUndoGroup();
                    }
                }
            }
        }
        void SliderPointerReleased(object? sender, PointerReleasedEventArgs args) {
            Log.Debug("Slider released");
            if (NotePropertiesViewModel.PanelControlPressed) {
                if (sender is Slider slider && slider.Tag is string tag && !string.IsNullOrEmpty(tag)) {
                    ViewModel.SetNoteParams(tag, (float)slider.Value);
                }
                NotePropertiesViewModel.PanelControlPressed = false;
                DocManager.Inst.EndUndoGroup();
            }
        }
        void SliderPointerMoved(object? sender, PointerEventArgs args) {
            if (sender is Slider slider && slider.Tag is string tag && !string.IsNullOrEmpty(tag)) {
                ViewModel.SetNoteParams(tag, (float)slider.Value);
            }
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
                }
            } else if (cmd is ConfigureExpressionsCommand) {
                LoadPart(null);
            }
        }
    }
}
