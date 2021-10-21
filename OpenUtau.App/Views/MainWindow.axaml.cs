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
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Formats;
using OpenUtau.Core.Ustx;
using Serilog;
using Point = Avalonia.Point;

namespace OpenUtau.App.Views {
    public partial class MainWindow : Window, ICmdSubscriber {
        private readonly KeyModifiers cmdKey =
            OS.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        private readonly MainWindowViewModel viewModel;

        private PianoRollWindow? pianoRollWindow;
        private bool openPianoRollWindow;

        private PartEditState? partEditState;
        private Rectangle? selectionBox;
        private DispatcherTimer timer;
        private bool forceClose;

        public MainWindow() {
            InitializeComponent();
            DataContext = viewModel = new MainWindowViewModel();
#if DEBUG
            this.AttachDevTools();
#endif
            viewModel.InitProject();
            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Tick += (sender, args) => PlaybackManager.Inst.UpdatePlayPos();
            timer.Start();
            Program.AutoUpdate?.Invoke();

            AddHandler(DragDrop.DropEvent, OnDrop);

            DocManager.Inst.AddSubscriber(this);
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
                     ThemeManager.GetString("errors.caption"),
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        void OnMainMenuOpened(object sender, RoutedEventArgs args) {
            viewModel.RefreshOpenRecent();
            viewModel.RefreshTemplates();
        }

        async void OnMenuSave(object sender, RoutedEventArgs args) => await Save();
        public async Task Save() {
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

        void OnMenuSaveTemplate(object sender, RoutedEventArgs args) {
            var project = DocManager.Inst.Project;
            var dialog = new TypeInDialog();
            dialog.Title = ThemeManager.GetString("menu.file.savetemplate");
            dialog.SetText("default");
            dialog.onFinish = file => {
                if (string.IsNullOrEmpty(file)) {
                    return;
                }
                file = System.IO.Path.GetFileNameWithoutExtension(file);
                file = $"{file}.ustx";
                file = System.IO.Path.Combine(PathManager.Inst.TemplatesPath, file);
                Ustx.Save(file, project.CloneAsTemplate());
            };
            dialog.ShowDialog(this);
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
                     ThemeManager.GetString("errors.caption"),
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
                     ThemeManager.GetString("errors.caption"),
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
                     ThemeManager.GetString("errors.caption"),
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        async void OnMenuExportAll(object sender, RoutedEventArgs args) {
            var project = DocManager.Inst.Project;
            if (await WarnToSave(project)) {
                PlaybackManager.Inst.RenderToFiles(project);
            }
        }

        async void OnMenuExportUst(object sender, RoutedEventArgs e) {
            var project = DocManager.Inst.Project;
            if (await WarnToSave(project)) {
                for (var i = 0; i < project.parts.Count; i++) {
                    var part = project.parts[i];
                    if (part is UVoicePart voicePart) {
                        var savePath = PathManager.Inst.GetPartSavePath(project.FilePath, i);
                        Ust.SavePart(project, voicePart, savePath);
                    }
                }
            }
        }

        private async Task<bool> WarnToSave(UProject project) {
            if (string.IsNullOrEmpty(project.FilePath)) {
                await MessageBox.Show(
                    this,
                    ThemeManager.GetString("dialogs.export.savefirst"),
                    ThemeManager.GetString("dialogs.export.caption"),
                    MessageBox.MessageBoxButtons.Ok);
                return false;
            }
            return true;
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

        void OnMenuWiki(object sender, RoutedEventArgs args) {
            OS.OpenWeb("https://github.com/stakira/OpenUtau/wiki");
        }

        void OnMenuVersion(object sender, RoutedEventArgs args) {
            OS.OpenWeb("https://github.com/stakira/OpenUtau/wiki");
        }

        void OnMenuLayoutVSplit11(object sender, RoutedEventArgs args) => LayoutSplit(null, 1.0 / 2);
        void OnMenuLayoutVSplit12(object sender, RoutedEventArgs args) => LayoutSplit(null, 1.0 / 3);
        void OnMenuLayoutVSplit13(object sender, RoutedEventArgs args) => LayoutSplit(null, 1.0 / 4);
        void OnMenuLayoutHSplit11(object sender, RoutedEventArgs args) => LayoutSplit(1.0 / 2, null);
        void OnMenuLayoutHSplit12(object sender, RoutedEventArgs args) => LayoutSplit(1.0 / 3, null);
        void OnMenuLayoutHSplit13(object sender, RoutedEventArgs args) => LayoutSplit(1.0 / 4, null);

        private void LayoutSplit(double? x, double? y) {
            var wa = Screens.Primary.WorkingArea;
            WindowState = WindowState.Normal;
            double borderThickness = (FrameSize!.Value.Width - ClientSize.Width) / 2;
            double titleBarHeight = FrameSize!.Value.Height - ClientSize.Height - borderThickness;
            Position = new PixelPoint(0, 0);
            Width = x != null ? wa.Size.Width * x.Value : wa.Size.Width;
            Height = (y != null ? wa.Size.Height * y.Value : wa.Size.Height) - titleBarHeight;
            if (pianoRollWindow != null) {
                pianoRollWindow.Position = new PixelPoint(x != null ? (int)Width : 0, y != null ? (int)(Height + (OS.IsMacOS() ? 25 : titleBarHeight)) : 0);
                pianoRollWindow.Width = x != null ? wa.Size.Width - Width : wa.Size.Width;
                pianoRollWindow.Height = (y != null ? wa.Size.Height - (Height + titleBarHeight) : wa.Size.Height) - titleBarHeight;
            }
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
            if (args.KeyModifiers == KeyModifiers.None) {
                switch (args.Key) {
                    case Key.Delete: viewModel.TracksViewModel.DeleteSelectedParts(); break;
                    case Key.Space: PlayOrPause(); break;
                    default: break;
                }
            } else if (args.KeyModifiers == KeyModifiers.Alt) {
                switch (args.Key) {
                    case Key.F4: ((IControlledApplicationLifetime)Application.Current.ApplicationLifetime).Shutdown(); break;
                    default: break;
                }
            } else if (args.KeyModifiers == cmdKey) {
                switch (args.Key) {
                    case Key.A: viewModel.TracksViewModel.SelectAllParts(); break;
                    case Key.N: viewModel.NewProject(); break;
                    case Key.O: Open(); break;
                    case Key.S: _ = Save(); break;
                    case Key.Z: viewModel.Undo(); break;
                    case Key.Y: viewModel.Redo(); break;
                    default: break;
                }
            }
            args.Handled = true;
        }

        async void OnDrop(object? sender, DragEventArgs args) {
            if (!args.Data.Contains(DataFormats.FileNames)) {
                return;
            }
            string file = args.Data.GetFileNames().FirstOrDefault();
            if (string.IsNullOrEmpty(file)) {
                return;
            }
            var ext = System.IO.Path.GetExtension(file);
            if (ext == ".ustx" || ext == ".ust" || ext == ".vsqx") {
                try {
                    viewModel.OpenProject(new string[] { file });
                } catch (Exception e) {
                    Log.Error(e, $"Failed to open file {file}");
                    _ = await MessageBox.Show(
                         this,
                         e.ToString(),
                         ThemeManager.GetString("errors.caption"),
                         MessageBox.MessageBoxButtons.Ok);
                }
            } else if (ext == ".zip" || ext == ".rar" || ext == ".uar") {
                var setup = new SingerSetupDialog() {
                    DataContext = new SingerSetupViewModel() {
                        ArchiveFilePath = file,
                    },
                };
                _ = setup.ShowDialog(this);
                if (setup.Position.Y < 0) {
                    setup.Position = setup.Position.WithY(0);
                }
            } else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".flac") {
                try {
                    viewModel.ImportAudio(file);
                } catch (Exception e) {
                    Log.Error(e, "Failed to import audio");
                    _ = await MessageBox.Show(
                         this,
                         e.ToString(),
                         ThemeManager.GetString("errors.caption"),
                         MessageBox.MessageBoxButtons.Ok);
                }
            }
        }

        void OnPlayOrPause(object sender, RoutedEventArgs args) {
            PlayOrPause();
        }

        void PlayOrPause() {
            if (!viewModel.PlaybackViewModel.PlayOrPause()) {
                MessageBox.Show(
                   this,
                   ThemeManager.GetString("dialogs.noresampler.message"),
                   ThemeManager.GetString("dialogs.noresampler.caption"),
                   MessageBox.MessageBoxButtons.Ok);
            }
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

        public void PartsCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            var control = canvas.InputHitTest(point.Position);
            if (partEditState != null) {
                return;
            }
            if (point.Properties.IsLeftButtonPressed) {
                if (args.KeyModifiers == cmdKey) {
                    // New selection.
                    viewModel.TracksViewModel.DeselectParts();
                    partEditState = new PartSelectionEditState(canvas, viewModel, GetSelectionBox(canvas));
                    Cursor = ViewConstants.cursorCross;
                } else if (args.KeyModifiers == (cmdKey | KeyModifiers.Shift)) {
                    // Additional selection.
                    partEditState = new PartSelectionEditState(canvas, viewModel, GetSelectionBox(canvas));
                    Cursor = ViewConstants.cursorCross;
                } else if (control == canvas) {
                    viewModel.TracksViewModel.DeselectParts();
                    var part = viewModel.TracksViewModel.MaybeAddPart(point.Position);
                    if (part != null) {
                        // Start moving right away
                        partEditState = new PartMoveEditState(canvas, viewModel, part);
                        Cursor = ViewConstants.cursorSizeAll;
                    }
                } else if (control is PartControl partControl) {
                    bool isVoice = partControl.part is UVoicePart;
                    bool isWave = partControl.part is UWavePart;
                    bool trim = point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin;
                    bool skip = point.Position.X < partControl.Bounds.Left + ViewConstants.ResizeMargin;
                    if (isVoice && trim) {
                        partEditState = new PartResizeEditState(canvas, viewModel, partControl.part);
                        Cursor = ViewConstants.cursorSizeWE;
                    } else if (isWave && skip) {
                        // TODO
                    } else if (isWave && trim) {
                        // TODO
                    } else {
                        partEditState = new PartMoveEditState(canvas, viewModel, partControl.part);
                        Cursor = ViewConstants.cursorSizeAll;
                    }
                }
            } else if (point.Properties.IsRightButtonPressed) {
                viewModel.TracksViewModel.DeselectParts();
                partEditState = new PartEraseEditState(canvas, viewModel);
                Cursor = ViewConstants.cursorNo;
            } else if (point.Properties.IsMiddleButtonPressed) {
                partEditState = new PartPanningState(canvas, viewModel);
                Cursor = ViewConstants.cursorHand;
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

        public void PartsCanvasPointerMoved(object sender, PointerEventArgs args) {
            var canvas = (Canvas)sender;
            var point = args.GetCurrentPoint(canvas);
            if (partEditState != null) {
                partEditState.Update(point.Pointer, point.Position);
                return;
            }
            var control = canvas.InputHitTest(point.Position);
            if (control is PartControl partControl) {
                bool isVoice = partControl.part is UVoicePart;
                bool isWave = partControl.part is UWavePart;
                bool trim = point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin;
                bool skip = point.Position.X < partControl.Bounds.Left + ViewConstants.ResizeMargin;
                if (isVoice && trim) {
                    Cursor = ViewConstants.cursorSizeWE;
                } else if (isWave && (skip || trim)) {
                    Cursor = null; // TODO
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
                        MainWindow = this,
                    };
                    pianoRollWindow.ViewModel.PlaybackViewModel = viewModel.PlaybackViewModel;
                }
                // Workaround for new window losing focus.
                openPianoRollWindow = true;
                DocManager.Inst.ExecuteCmd(new LoadPartNotification(partControl.part, DocManager.Inst.Project));
            }
        }

        public void PartsCanvasPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var delta = args.Delta;
            if (args.KeyModifiers == KeyModifiers.None || args.KeyModifiers == KeyModifiers.Shift) {
                if (args.KeyModifiers.HasFlag(KeyModifiers.Shift)) {
                    delta = new Vector(delta.Y, delta.X);
                }
                if (delta.X != 0) {
                    var scrollbar = this.FindControl<ScrollBar>("HScrollBar");
                    scrollbar.Value = Math.Max(scrollbar.Minimum,
                        Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * delta.X));
                }
                if (delta.Y != 0) {
                    var scrollbar = this.FindControl<ScrollBar>("VScrollBar");
                    scrollbar.Value = Math.Max(scrollbar.Minimum,
                        Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * delta.Y));
                }
            } else if (args.KeyModifiers == KeyModifiers.Alt) {
                var scaler = this.FindControl<ViewScaler>("VScaler");
                ViewScalerPointerWheelChanged(scaler, args);
            } else if (args.KeyModifiers == cmdKey) {
                var timelineCanvas = this.FindControl<Canvas>("TimelineCanvas");
                TimelinePointerWheelChanged(timelineCanvas, args);
            }
        }

        public async void WindowClosing(object? sender, CancelEventArgs e) {
            if (!forceClose && !DocManager.Inst.ChangesSaved) {
                e.Cancel = true;
                var result = await MessageBox.Show(
                    this,
                    ThemeManager.GetString("dialogs.exitsave.message"),
                    ThemeManager.GetString("dialogs.exitsave.caption"),
                    MessageBox.MessageBoxButtons.YesNoCancel);
                switch (result) {
                    case MessageBox.MessageBoxResult.Yes:
                        await Save();
                        goto case MessageBox.MessageBoxResult.No;
                    case MessageBox.MessageBoxResult.No:
                        pianoRollWindow?.Close();
                        forceClose = true;
                        Close();
                        break;
                    default:
                        break;
                }
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UserMessageNotification userMessage) {
                MessageBox.Show(
                    this,
                    userMessage.message,
                    ThemeManager.GetString("errors.caption"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }
    }
}
