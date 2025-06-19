using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingerConfigDialog : Window {

        public SingerConfigDialog() {
            InitializeComponent();
        }

        void OnSave(object sender, RoutedEventArgs e) {
            if ((DataContext as SingerConfigViewModel)!.Save()) {
                Close();
            }
        }

        void OnCancel(object sender, RoutedEventArgs e) {
            Close();
        }

        async void OpenFile(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            SingerConfigViewModel vm = (DataContext as SingerConfigViewModel)!;
            FilePickerFileType fileTypes;
            switch (button.Tag) {
                case "image":
                    fileTypes = FilePickerFileTypes.ImageAll;
                    break;
                case "portrait":
                    fileTypes = FilePickerFileTypes.ImageAll;
                    break;
                default:
                    fileTypes = FilePicker.AudioFiles;
                    break;
            }

            var path = await FilePicker.OpenFile(this, "menu.file.open", vm.Location, fileTypes);
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (File.Exists(path)) {
                string relPath = Path.GetRelativePath(vm.Location, path);
                try {
                    //If the image isn't inside the voicebank, copy it in.
                    if (!path.StartsWith(vm.Location)) {
                        relPath = Path.GetFileName(path);
                        string newFile = Path.Combine(vm.Location, relPath);
                        File.Copy(path, newFile, true);
                    }
                } catch (Exception ex) {
                    Log.Error(ex, "Failed to copy");
                    _ = await MessageBox.ShowError(this, ex);
                }

                switch (button.Tag) {
                    case "image":
                        vm.Image = relPath;
                        break;
                    case "portrait":
                        vm.Portrait = relPath;
                        break;
                    case "sample":
                        vm.Sample = relPath;
                        break;
                }
            }
        }

        void DeleteFile(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            SingerConfigViewModel vm = (DataContext as SingerConfigViewModel)!;

            switch (button.Tag) {
                case "image":
                    vm.Image = string.Empty;
                    break;
                case "portrait":
                    vm.Portrait = string.Empty;
                    break;
                case "sample":
                    vm.Sample = string.Empty;
                    break;
            }
        }

        void TextBox_KeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
            }
        }

        void OpenWeb(object? sender, RoutedEventArgs e) {
            SingerConfigViewModel vm = (DataContext as SingerConfigViewModel)!;
            if (string.IsNullOrWhiteSpace(vm.Web)) {
                return;
            }
            try {
                OS.OpenWeb(vm.Web);
            } catch (Exception ex) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(ex));
            }
        }
    }
}
