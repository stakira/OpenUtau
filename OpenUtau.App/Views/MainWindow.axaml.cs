using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class MainWindow : Window {
        private readonly MainWindowViewModel viewModel;
        private object midiWindow;

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

        void OnEditTempo(object sender, PointerPressedEventArgs args) {

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
        }

        void OnMenuSingers(object sender, RoutedEventArgs args) {
            var dialog = new SingersDialog() {
                DataContext = new SingersViewModel(),
            };
            dialog.ShowDialog(this);
        }

        async void OnMenuInstallSinger(object sender, RoutedEventArgs args) {
            var dialog = new OpenFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Name = "Archive File",
                        Extensions = new List<string>(){ "zip", "rar", "7z", "uar" },
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
                        Extensions = new List<string>(){ "zip", "rar", "7z", "uar" },
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
        }

        void OnMenuPreferences(object sender, RoutedEventArgs args) {
            var dialog = new PreferencesDialog() {
                DataContext = new PreferencesViewModel(),
            };
            dialog.ShowDialog(this);
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

        public void ZoomerPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            viewModel.TracksViewModel.OnYZoomed(new Point(0, 0.5), 0.1 * args.Delta.Y);
        }

        public void TrackHeaderCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            viewModel.TracksViewModel.AddTrack();
        }

        public void PartsCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
            if (!(sender is Canvas canvas)) {
                return;
            }
            var position = args.GetPosition(canvas);
            var control = canvas.InputHitTest(position);
            if (control == canvas) {
                viewModel.TracksViewModel.MaybeAddPart(position);
            }
        }

        public void PartsCanvasDoubleTapped(object sender, RoutedEventArgs args) {
            if (!(sender is Canvas canvas)) {
                return;
            }
            var e = (TappedEventArgs)args;
            var control = canvas.InputHitTest(e.GetPosition(canvas));
            if (control is PartControl partControl) {
            }
        }
    }
}
