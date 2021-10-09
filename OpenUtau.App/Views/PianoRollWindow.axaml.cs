using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PianoRollWindow : Window {
        public MainWindow? MainWindow { get; set; }
        private PianoRollViewModel ViewModel => (PianoRollViewModel)DataContext!;

        private NoteEditState? noteEditState;
        private Rectangle? selectionBox;
        private Border? expValueTip;

        public PianoRollWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            expValueTip = this.FindControl<Border>("ExpValueTip");
        }

        public void HScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var scrollbar = (ScrollBar)sender;
            scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * args.Delta.Y));
        }

        public void VScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var scrollbar = (ScrollBar)sender;
            scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * args.Delta.Y));
        }

        public void TimelinePointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var canvas = (Canvas)sender;
            var position = args.GetCurrentPoint((IVisual)sender).Position;
            var size = canvas.Bounds.Size;
            position = position.WithX(position.X / size.Width).WithY(position.Y / size.Height);
            ViewModel.NotesViewModel.OnXZoomed(position, 0.1 * args.Delta.Y);
        }

        public void ViewScalerPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            ViewModel.NotesViewModel.OnYZoomed(new Point(0, 0.5), 0.1 * args.Delta.Y);
        }

        public void TimelinePointerPressed(object sender, PointerPressedEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (point.Properties.IsLeftButtonPressed) {
                args.Pointer.Capture(canvas);
                int tick = ViewModel.NotesViewModel.PointToSnappedTick(point.Position);
                ViewModel.PlaybackViewModel.MovePlayPos(tick);
            }
        }

        public void TimelinePointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (point.Properties.IsLeftButtonPressed) {
                int tick = ViewModel.NotesViewModel.PointToSnappedTick(point.Position);
                ViewModel.PlaybackViewModel.MovePlayPos(tick);
            }
        }

        public void TimelinePointerReleased(object sender, PointerReleasedEventArgs args) {
            args.Pointer.Capture(null);
        }

        public void NotesCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
            if (ViewModel.NotesViewModel.Part == null) {
                return;
            }
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (noteEditState != null) {
                return;
            }
            if (point.Properties.IsLeftButtonPressed) {
                NotesCanvasLeftPointerPressed(canvas, point, args);
            } else if (point.Properties.IsRightButtonPressed) {
                ViewModel.NotesViewModel.DeselectNotes();
                noteEditState = new NoteEraseEditState(canvas, ViewModel);
                Cursor = ViewConstants.cursorNo;
            } else if (point.Properties.IsMiddleButtonPressed) {
                noteEditState = new NotePanningState(canvas, ViewModel);
                Cursor = ViewConstants.cursorHand;
            }
            if (noteEditState != null) {
                noteEditState.Begin(point.Pointer, point.Position);
                noteEditState.Update(point.Pointer, point.Position);
            }
        }

        private void NotesCanvasLeftPointerPressed(Canvas canvas, PointerPoint point, PointerPressedEventArgs args) {
            var pitHitInfo = ViewModel.NotesViewModel.HitTest.HitTestPitchPoint(point.Position);
            if (pitHitInfo.Note != null) {
                noteEditState = new PitchPointEditState(canvas, ViewModel,
                    pitHitInfo.Note, pitHitInfo.Index, pitHitInfo.OnPoint, pitHitInfo.X, pitHitInfo.Y);
                Cursor = ViewConstants.cursorHand;
                return;
            }
            var noteHitInfo = ViewModel.NotesViewModel.HitTest.HitTestNote(point.Position);
            if (args.KeyModifiers == KeyModifiers.Control) {
                // New selection.
                ViewModel.NotesViewModel.DeselectNotes();
                noteEditState = new NoteSelectionEditState(canvas, ViewModel, GetSelectionBox(canvas));
                Cursor = ViewConstants.cursorCross;
                return;
            }
            if (args.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift)) {
                // Additional selection.
                noteEditState = new NoteSelectionEditState(canvas, ViewModel, GetSelectionBox(canvas));
                Cursor = ViewConstants.cursorCross;
                return;
            }
            if (noteHitInfo.hitBody) {
                if (noteHitInfo.hitResizeArea) {
                    noteEditState = new NoteResizeEditState(canvas, ViewModel, noteHitInfo.note);
                    Cursor = ViewConstants.cursorSizeWE;
                } else {
                    noteEditState = new NoteMoveEditState(canvas, ViewModel, noteHitInfo.note);
                    Cursor = ViewConstants.cursorSizeAll;
                }
                return;
            }
            ViewModel.NotesViewModel.DeselectNotes();
            var note = ViewModel.NotesViewModel.MaybeAddNote(point.Position);
            if (note != null) {
                // Start moving right away
                noteEditState = new NoteMoveEditState(canvas, ViewModel, note);
                Cursor = ViewConstants.cursorSizeAll;
            }
        }

        private Rectangle GetSelectionBox(Canvas canvas) {
            if (selectionBox != null) {
                return selectionBox;
            }
            selectionBox = new Rectangle() {
                Stroke = ThemeManager.ForegroundBrush,
                StrokeThickness = 2,
                Fill = ThemeManager.TickLineBrushLow,
                // radius = 8
                IsHitTestVisible = false,
            };
            canvas.Children.Add(selectionBox);
            selectionBox.ZIndex = 1000;
            return selectionBox;
        }

        public void NotesCanvasPointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (noteEditState != null) {
                noteEditState.Update(point.Pointer, point.Position);
                return;
            }
            if (ViewModel?.NotesViewModel?.HitTest == null) {
                return;
            }
            var pitHitInfo = ViewModel.NotesViewModel.HitTest.HitTestPitchPoint(point.Position);
            if (pitHitInfo.Note != null) {
                Cursor = ViewConstants.cursorHand;
                return;
            }
            var noteHitInfo = ViewModel.NotesViewModel.HitTest.HitTestNote(point.Position);
            if (noteHitInfo.hitResizeArea) {
                Cursor = ViewConstants.cursorSizeWE;
                return;
            }
            Cursor = null;
        }

        public void NotesCanvasPointerReleased(object sender, PointerReleasedEventArgs args) {
            if (noteEditState == null) {
                return;
            }
            if (noteEditState.MouseButton != args.InitialPressMouseButton) {
                return;
            }
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            noteEditState.Update(point.Pointer, point.Position);
            noteEditState.End(point.Pointer, point.Position);
            noteEditState = null;
            Cursor = null;
        }

        public void NotesCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            if (!(sender is Canvas canvas)) {
                return;
            }
            var e = (TappedEventArgs)args;
        }

        public void NotesCanvasPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            if (args.KeyModifiers == KeyModifiers.None) {
                var scrollbar = this.FindControl<ScrollBar>("VScrollBar");
                VScrollPointerWheelChanged(scrollbar, args);
            } else if (args.KeyModifiers == KeyModifiers.Control) {
                var scaler = this.FindControl<ViewScaler>("VScaler");
                ViewScalerPointerWheelChanged(scaler, args);
            } else if (args.KeyModifiers == KeyModifiers.Shift) {
                var scrollbar = this.FindControl<ScrollBar>("HScrollBar");
                HScrollPointerWheelChanged(scrollbar, args);
            } else if (args.KeyModifiers == (KeyModifiers.Shift | KeyModifiers.Control)) {
                var canvas = this.FindControl<Canvas>("TimelineCanvas");
                TimelinePointerWheelChanged(canvas, args);
            }
        }

        public void ExpCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
            if (ViewModel.NotesViewModel.Part == null) {
                return;
            }
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (noteEditState != null) {
                return;
            }
            if (point.Properties.IsLeftButtonPressed) {
                noteEditState = new ExpSetValueState(canvas, ViewModel, expValueTip!);
            } else if (point.Properties.IsRightButtonPressed) {
                noteEditState = new ExpResetValueState(canvas, ViewModel);
                Cursor = ViewConstants.cursorNo;
            }
            if (noteEditState != null) {
                noteEditState.Begin(point.Pointer, point.Position);
                noteEditState.Update(point.Pointer, point.Position);
            }
        }

        public void ExpCanvasPointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (noteEditState != null) {
                noteEditState.Update(point.Pointer, point.Position);
            }
        }

        public void ExpCanvasPointerReleased(object sender, PointerReleasedEventArgs args) {
            if (noteEditState == null) {
                return;
            }
            if (noteEditState.MouseButton != args.InitialPressMouseButton) {
                return;
            }
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            noteEditState.Update(point.Pointer, point.Position);
            noteEditState.End(point.Pointer, point.Position);
            noteEditState = null;
            Cursor = null;
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
            var notesVm = ViewModel.NotesViewModel;
            if (notesVm.Part == null) {
                return;
            }
            if (args.KeyModifiers == KeyModifiers.None) {
                switch (args.Key) {
                    case Key.Delete: notesVm.DeleteSelectedNotes(); break;
                    case Key.U: notesVm.ShowVibrato = !notesVm.ShowVibrato; break;
                    case Key.I: notesVm.ShowPitch = !notesVm.ShowPitch; break;
                    case Key.O: notesVm.ShowPhoneme = !notesVm.ShowPhoneme; break;
                    case Key.P: notesVm.IsSnapOn = !notesVm.IsSnapOn; break;
                    case Key.T: notesVm.ShowTips = !notesVm.ShowTips; break;
                    case Key.Up: notesVm.TransposeSelection(1); break;
                    case Key.Down: notesVm.TransposeSelection(-1); break;
                    case Key.Space: break;
                    default: break;
                }
            } else if (args.KeyModifiers == KeyModifiers.Control) {
                switch (args.Key) {
                    case Key.A: notesVm.SelectAllNotes(); break;
                    case Key.S: _ = MainWindow?.Save(); break;
                    case Key.Z: ViewModel.Undo(); break;
                    case Key.Y: ViewModel.Redo(); break;
                    case Key.C: notesVm.CopyNotes(); break;
                    case Key.X: notesVm.CutNotes(); break;
                    case Key.V: notesVm.PasteNotes(); break;
                    case Key.Up: notesVm.TransposeSelection(12); break;
                    case Key.Down: notesVm.TransposeSelection(-12); break;
                    default: break;
                }
            } else if (args.KeyModifiers == KeyModifiers.Alt) {
                switch (args.Key) {
                    case Key.F4:
                        Hide();
                        break;
                    default:
                        break;
                }
            }
        }

        void WindowClosing(object? sender, CancelEventArgs e) {
            Hide();
            e.Cancel = true;
        }
    }
}
