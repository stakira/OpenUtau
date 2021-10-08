using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PianoRollWindow : Window {
        private PianoRollViewModel ViewModel => (PianoRollViewModel)DataContext!;

        private NoteEditState? noteEditState;
        private Rectangle? selectionBox;

        public PianoRollWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
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
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            var control = canvas.InputHitTest(point.Position);
            if (noteEditState != null) {
                return;
            }
            if (point.Properties.IsLeftButtonPressed) {
                if (args.KeyModifiers == KeyModifiers.Control) {
                    // New selection.
                    ViewModel.NotesViewModel.DeselectNotes();
                    noteEditState = new NoteSelectionEditState(canvas, ViewModel, GetSelectionBox(canvas));
                    Cursor = ViewConstants.cursorCross;
                } else if (args.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift)) {
                    // Additional selection.
                    noteEditState = new NoteSelectionEditState(canvas, ViewModel, GetSelectionBox(canvas));
                    Cursor = ViewConstants.cursorCross;
                } else if (control == canvas) {
                    ViewModel.NotesViewModel.DeselectNotes();
                    var note = ViewModel.NotesViewModel.MaybeAddNote(point.Position);
                    if (note != null) {
                        // Start moving right away
                        //noteEditState = new NoteMoveEditState(canvas, ViewModel, note);
                        //Cursor = ViewConstants.cursorSizeAll;
                    }
                } else if (control is PartControl partControl) {
                    // TODO: edit part name
                    if (point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin) {
                        //noteEditState = new PartResizeEditState(canvas, ViewModel, partControl.part);
                        //Cursor = ViewConstants.cursorSizeWE;
                    } else {
                        //noteEditState = new NoteMoveEditState(canvas, ViewModel, partControl.part);
                        //Cursor = ViewConstants.cursorSizeAll;
                    }
                }
            } else if (point.Properties.IsRightButtonPressed) {
                //ViewModel.NotesViewModel.DeselectNotes();
                //noteEditState = new PartEraseEditState(canvas, ViewModel);
                //Cursor = ViewConstants.cursorNo;
            } else if (point.Properties.IsMiddleButtonPressed) {
                //noteEditState = new PartPanningState(canvas, ViewModel);
                //Cursor = ViewConstants.cursorHand;
            }
            if (noteEditState != null) {
                noteEditState.Begin(point.Pointer, point.Position);
                noteEditState.Update(point.Pointer, point.Position);
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
            var control = canvas.InputHitTest(point.Position);
            if (control is PartControl partControl) {
                if (point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin) {
                    Cursor = ViewConstants.cursorSizeWE;
                } else {
                    Cursor = null;
                }
            } else {
                Cursor = null;
            }
        }

        public void NotesCanvasPointerReleased(object sender, PointerReleasedEventArgs args) {
            if (noteEditState != null) {
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
        }

        public void NotesCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            if (!(sender is Canvas canvas)) {
                return;
            }
            var e = (TappedEventArgs)args;
        }

        public void NotesCanvasPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            if (args.KeyModifiers == KeyModifiers.Control) {
                var canvas = this.FindControl<Canvas>("TimelineCanvas");
                TimelinePointerWheelChanged(canvas, args);
            } else if (args.KeyModifiers == KeyModifiers.Shift) {
                var scrollbar = this.FindControl<ScrollBar>("HScrollBar");
                HScrollPointerWheelChanged(scrollbar, args);
            } else if (args.KeyModifiers == (KeyModifiers.Shift | KeyModifiers.Control)) {
                var scaler = this.FindControl<ViewScaler>("VScaler");
                ViewScalerPointerWheelChanged(scaler, args);
            } else if (args.KeyModifiers == KeyModifiers.None) {
                var scrollbar = this.FindControl<ScrollBar>("VScrollBar");
                VScrollPointerWheelChanged(scrollbar, args);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
        }

        void WindowClosing(object? sender, CancelEventArgs e) {
            Hide();
            e.Cancel = true;
        }
    }
}
