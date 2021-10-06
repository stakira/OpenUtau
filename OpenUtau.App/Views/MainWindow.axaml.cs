using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;
using Point = Avalonia.Point;

namespace OpenUtau.App.Views {
    public partial class MainWindow : Window {
        private readonly MainWindowViewModel viewModel;

        private PianoRollWindow? pianoRollWindow;
        private bool openPianoRollWindow;

        private Cursor cursorCross = new Cursor(StandardCursorType.Cross);
        private Cursor cursorHand = new Cursor(StandardCursorType.Hand);
        private Cursor cursorNo = new Cursor(StandardCursorType.No);
        private Cursor cursorSizeAll = new Cursor(StandardCursorType.SizeAll);
        private Cursor cursorSizeWE = new Cursor(StandardCursorType.SizeWestEast);

        private PartEditState? partEditState;
        private Rectangle? selectionBox;

        public MainWindow() {
            InitializeComponent();
            DataContext = viewModel = new MainWindowViewModel();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnEditTimeSignature(object sender, PointerPressedEventArgs args) {
            var project = DocManager.Inst.Project;
            var dialog = new TypeInDialog();
            dialog.Title = "Time Signature";
            dialog.SetText($"{project.beatPerBar}/{project.beatUnit}");
            dialog.onFinish = s => {
                var parts = s.Split('/');
                int beatPerBar = parts.Length > 0 && int.TryParse(parts[0], out beatPerBar) ? beatPerBar : project.beatPerBar;
                int beatUnit = parts.Length > 1 && int.TryParse(parts[1], out beatUnit) ? beatUnit : project.beatUnit;
                viewModel.PlaybackViewModel.SetTimeSignature(beatPerBar, beatUnit);
            };
            dialog.ShowDialog(this);
        }

        void OnEditBpm(object sender, PointerPressedEventArgs args) {
            var project = DocManager.Inst.Project;
            var dialog = new TypeInDialog();
            dialog.Title = "BPM";
            dialog.SetText(project.bpm.ToString());
            dialog.onFinish = s => {
                if (double.TryParse(s, out double bpm)) {
                    viewModel.PlaybackViewModel.SetBpm(bpm);
                }
            };
            dialog.ShowDialog(this);
        }

        void OnMenuNew(object sender, RoutedEventArgs args) {
            viewModel.NewProject();
        }

        void OnMenuOpen(object sender, RoutedEventArgs args) => Open();
        async void Open() {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Project Files",
                        Extensions = new List<string>(){ "ustx", "vsqx", "ust" },
                    },
                },
                AllowMultiple = true,
            };
            var files = await dialog.ShowAsync(this);
            try {
                viewModel.OpenProject(files);
            } catch (Exception e) {
                Log.Error(e, $"Failed to open files {string.Join("\n", files)}");
                _ = await MessageBox.Show(
                     this,
                     e.ToString(),
                     "errors.caption",
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        async void OnMenuSave(object sender, RoutedEventArgs args) => await Save();
        async Task Save() {
            if (!viewModel.ProjectSaved) {
                await SaveAs();
            } else {
                viewModel.SaveProject();
            }
        }

        async void OnMenuSaveAs(object sender, RoutedEventArgs args) => await SaveAs();
        async Task SaveAs() {
            SaveFileDialog dialog = new SaveFileDialog() {
                DefaultExtension = "ustx",
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Project Files",
                        Extensions = new List<string>(){ "ustx" },
                    },
                },
                Title = "Save As",
            };
            viewModel.SaveProject(await dialog.ShowAsync(this));
        }

        async void OnMenuImportTracks(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Project Files",
                        Extensions = new List<string>(){ "ustx", "vsqx", "ust" },
                    },
                },
                AllowMultiple = true,
            };
            try {
                viewModel.ImportTracks(await dialog.ShowAsync(this));
            } catch (Exception e) {
                Log.Error(e, $"Failed to import files");
                _ = await MessageBox.Show(
                     this,
                     e.ToString(),
                     "errors.caption",
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        async void OnMenuImportAudio(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Audio Files",
                        Extensions = new List<string>(){ "wav", "mp3", "ogg", "flac" },
                    },
                },
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(this);
            if (files == null || files.Length != 1) {
                return;
            }
            try {
                viewModel.ImportAudio(files[0]);
            } catch (Exception e) {
                Log.Error(e, "Failed to import audio");
                _ = await MessageBox.Show(
                     this,
                     e.ToString(),
                     "errors.caption",
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        async void OnMenuImportMidi(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Midi File",
                        Extensions = new List<string>(){ "mid" },
                    },
                },
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(this);
            if (files == null || files.Length != 1) {
                return;
            }
            try {
                viewModel.ImportMidi(files[0]);
            } catch (Exception e) {
                Log.Error(e, "Failed to import midi");
                _ = await MessageBox.Show(
                     this,
                     e.ToString(),
                     "errors.caption",
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        void OnMenuExpressionss(object sender, RoutedEventArgs args) {
            var dialog = new ExpressionsDialog() {
                DataContext = new ExpressionsViewModel(),
            };
            dialog.ShowDialog(this);
            if (dialog.Position.Y < 0) {
                dialog.Position = dialog.Position.WithY(0);
            }
        }

        void OnMenuSingers(object sender, RoutedEventArgs args) {
            var dialog = new SingersDialog() {
                DataContext = new SingersViewModel(),
            };
            dialog.ShowDialog(this);
            if (dialog.Position.Y < 0) {
                dialog.Position = dialog.Position.WithY(0);
            }
        }

        async void OnMenuInstallSinger(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Archive File",
                        Extensions = new List<string>(){ "zip", "rar", "uar" },
                    },
                },
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(this);
            if (files == null || files.Length != 1) {
                return;
            }
            _ = Task.Run(() => {
                var task = Task.Run(() => {
                    viewModel.InstallSinger(files[0]);
                });
                try {
                    task.Wait();
                } catch (AggregateException ae) {
                    Log.Error(ae, "Failed to install singer");
                    MessageBox.Show(
                        this,
                        ae.Flatten().InnerExceptions.First().ToString(),
                        "errors.caption",
                        MessageBox.MessageBoxButtons.Ok);
                }
            });
        }

        async void OnMenuInstallSingerAdvanced(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Archive File",
                        Extensions = new List<string>(){ "zip", "rar", "uar" },
                    },
                },
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(this);
            if (files == null || files.Length != 1) {
                return;
            }
            var setup = new SingerSetupDialog() {
                DataContext = new SingerSetupViewModel() {
                    ArchiveFilePath = files[0],
                },
            };
            _ = setup.ShowDialog(this);
            if (setup.Position.Y < 0) {
                setup.Position = setup.Position.WithY(0);
            }
        }

        void OnMenuPreferences(object sender, RoutedEventArgs args) {
            var dialog = new PreferencesDialog() {
                DataContext = new PreferencesViewModel(),
            };
            dialog.ShowDialog(this);
            if (dialog.Position.Y < 0) {
                dialog.Position = dialog.Position.WithY(0);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
            if (args.KeyModifiers == KeyModifiers.None) {
                switch (args.Key) {
                    case Key.Delete:
                        // TODO
                        break;
                    case Key.Space:
                        PlayOrPause();
                        break;
                    default:
                        break;
                }
            } else if (args.KeyModifiers == KeyModifiers.Alt) {
                switch (args.Key) {
                    case Key.F4:
                        ((IControlledApplicationLifetime)Application.Current.ApplicationLifetime).Shutdown();
                        break;
                    default:
                        break;
                }
            } else if (args.KeyModifiers == KeyModifiers.Control) {
                switch (args.Key) {
                    case Key.A:
                        viewModel.TracksViewModel.SelectAllParts();
                        break;
                    case Key.N:
                        viewModel.NewProject();
                        break;
                    case Key.O:
                        Open();
                        break;
                    case Key.S:
                        _ = Save();
                        break;
                    case Key.Z:
                        viewModel.Undo();
                        break;
                    case Key.Y:
                        viewModel.Redo();
                        break;
                    default:
                        break;
                }
            }
            args.Handled = true;
        }

        void OnPlayOrPause(object sender, RoutedEventArgs args) {
            PlayOrPause();
        }

        void PlayOrPause() {
            if (!viewModel.PlaybackViewModel.PlayOrPause()) {
                MessageBox.Show(
                   this,
                   "dialogs.noresampler.message",
                   "dialogs.noresampler.caption",
                   MessageBox.MessageBoxButtons.Ok);
            }
        }

        public void HScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var scrollbar = (ScrollBar)sender;
            scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - 0.25 * scrollbar.SmallChange * args.Delta.Y));
        }

        public void VScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var scrollbar = (ScrollBar)sender;
            scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - 0.25 * scrollbar.SmallChange * args.Delta.Y));
        }

        public void TimelinePointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var canvas = (Canvas)sender;
            var position = args.GetCurrentPoint((IVisual)sender).Position;
            var size = canvas.Bounds.Size;
            position = position.WithX(position.X / size.Width).WithY(position.Y / size.Height);
            viewModel.TracksViewModel.OnXZoomed(position, 0.1 * args.Delta.Y);
        }

        public void ViewScalerPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            viewModel.TracksViewModel.OnYZoomed(new Point(0, 0.5), 0.1 * args.Delta.Y);
        }

        public void TimelinePointerPressed(object sender, PointerPressedEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (point.Properties.IsLeftButtonPressed) {
                args.Pointer.Capture(canvas);
                int tick = viewModel.TracksViewModel.PointToSnappedTick(point.Position);
                viewModel.PlaybackViewModel.MovePlayPos(tick);
            }
        }

        public void TimelinePointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (point.Properties.IsLeftButtonPressed) {
                int tick = viewModel.TracksViewModel.PointToSnappedTick(point.Position);
                viewModel.PlaybackViewModel.MovePlayPos(tick);
            }
        }

        public void TimelinePointerReleased(object sender, PointerReleasedEventArgs args) {
            args.Pointer.Capture(null);
        }

        public void TrackHeaderCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            viewModel.TracksViewModel.AddTrack();
        }

        public void PartsCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            var control = canvas.InputHitTest(point.Position);
            if (partEditState != null) {
                return;
            }
            if (point.Properties.IsLeftButtonPressed) {
                if (args.KeyModifiers == KeyModifiers.Control) {
                    // New selection.
                    viewModel.TracksViewModel.DeselectParts();
                    partEditState = new PartSelectionEditState(canvas, viewModel, GetSelectionBox(canvas));
                    Cursor = cursorCross;
                } else if (args.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift)) {
                    // Additional selection.
                    partEditState = new PartSelectionEditState(canvas, viewModel, GetSelectionBox(canvas));
                    Cursor = cursorCross;
                } else if (control == canvas) {
                    viewModel.TracksViewModel.DeselectParts();
                    var part = viewModel.TracksViewModel.MaybeAddPart(point.Position);
                    if (part != null) {
                        // Start moving right away
                        partEditState = new PartMoveEditState(canvas, viewModel, part);
                        Cursor = cursorSizeAll;
                    }
                } else if (control is PartControl partControl) {
                    // TODO: edit part name
                    if (point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin) {
                        partEditState = new PartResizeEditState(canvas, viewModel, partControl.part);
                        Cursor = cursorSizeWE;
                    } else {
                        partEditState = new PartMoveEditState(canvas, viewModel, partControl.part);
                        Cursor = cursorSizeAll;
                    }
                }
            } else if (point.Properties.IsRightButtonPressed) {
                viewModel.TracksViewModel.DeselectParts();
                partEditState = new PartEraseEditState(canvas, viewModel);
                Cursor = cursorNo;
            } else if (point.Properties.IsMiddleButtonPressed) {
                partEditState = new PartPanningState(canvas, viewModel);
                Cursor = cursorHand;
            }
            if (partEditState != null) {
                partEditState.Begin(point.Pointer, point.Position);
                partEditState.Update(point.Pointer, point.Position);
            }
        }

        private Rectangle GetSelectionBox(Canvas canvas) {
            if (selectionBox != null) {
                return selectionBox;
            }
            selectionBox = new Rectangle() {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = ThemeManager.TickLineBrushLow,
                // radius = 8
                IsHitTestVisible = false,
            };
            canvas.Children.Add(selectionBox);
            selectionBox.ZIndex = 1000;
            return selectionBox;
        }

        public void PartsCanvasPointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (partEditState != null) {
                partEditState.Update(point.Pointer, point.Position);
                return;
            }
            var control = canvas.InputHitTest(point.Position);
            if (control is PartControl partControl) {
                if (point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin) {
                    Cursor = cursorSizeWE;
                } else {
                    Cursor = null;
                }
            } else {
                Cursor = null;
            }
        }

        public void PartsCanvasPointerReleased(object sender, PointerReleasedEventArgs args) {
            if (partEditState != null) {
                if (partEditState.MouseButton != args.InitialPressMouseButton) {
                    return;
                }
                var canvas = (Canvas)sender;
                var point = args.GetCurrentPoint(canvas);
                partEditState.Update(point.Pointer, point.Position);
                partEditState.End(point.Pointer, point.Position);
                partEditState = null;
                Cursor = null;
            }
            if (openPianoRollWindow) {
                pianoRollWindow?.Show();
                pianoRollWindow?.Activate();
                openPianoRollWindow = false;
            }
        }

        public void PartsCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            if (!(sender is Canvas canvas)) {
                return;
            }
            var e = (TappedEventArgs)args;
            var control = canvas.InputHitTest(e.GetPosition(canvas));
            if (control is PartControl partControl) {
                if (pianoRollWindow == null) {
                    pianoRollWindow = new PianoRollWindow() {
                        DataContext = new PianoRollViewModel() {
                            NotesViewModel = new NotesViewModel(),
                            PlaybackViewModel = viewModel.PlaybackViewModel,
                        }
                    };
                }
                // Workaround for new window losing focus.
                openPianoRollWindow = true;
                DocManager.Inst.ExecuteCmd(new LoadPartNotification(partControl.part, DocManager.Inst.Project));
            }
        }

        public void PartsCanvasWheelChanged(object sender, PointerWheelEventArgs args) {
            if (args.KeyModifiers == KeyModifiers.Control) {
                var canvas = this.FindControl<Canvas>("TimelineCanvas");
                TimelinePointerWheelChanged(canvas, args);
            } else if (args.KeyModifiers == KeyModifiers.Shift) {
                var scrollbar = this.FindControl<ScrollBar>("HScrollBar");
                HScrollPointerWheelChanged(scrollbar, args);
            } else if (args.KeyModifiers == KeyModifiers.None) {
                var scrollbar = this.FindControl<ScrollBar>("VScrollBar");
                VScrollPointerWheelChanged(scrollbar, args);
            }
        }

        public void WindowClosing(object? sender, CancelEventArgs e) {
            if (!DocManager.Inst.ChangesSaved) {
                //e.Cancel = true;
            }
        }
    }
}
