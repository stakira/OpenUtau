using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using Avalonia.Media;
using System.Collections.Generic;
using System;

namespace OpenUtau.App.ViewModels {
    class RemarkViewModel : ViewModelBase {
        [Reactive] public string? Text { get; set; }
        private IBrush _selectedColor;
        private List<IBrush> _colors;
        

        private UPart part;
        private URemark remark;
        private int index;
        private int postion;
        public string Title { get; set; } = "";
        public IBrush SelectedColor {
            get => _selectedColor;
            set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
        }
        public List<IBrush> RemarkColors {
            get => _colors;
            set => this.RaiseAndSetIfChanged(ref _colors, value);
        }
        public RemarkViewModel(UPart part, URemark remark, int postion, int index = -1) {
            this.part = part;
            this.remark = remark;
            this.postion = postion;
            this.index = index;
            this.Text = remark.text;

            _colors = new List<IBrush>
            {
                Brushes.Red,
                Brushes.Green,
                Brushes.Blue,
                Brushes.Yellow,
                Brushes.Purple,
                Brushes.Orange,
                Brushes.Pink,
                Brushes.Magenta,
                Brushes.LightGreen,
                Brushes.DarkGreen
            };
            
            try {
                _selectedColor = Brush.Parse(remark.color);
            } catch (FormatException) {
                _selectedColor = Brushes.Red;
            }
        }
        public void DeleteRemark() {
            if (index == -1) return;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new DeleteRemarkCommand(part, part.remarks[index], index));
            DocManager.Inst.EndUndoGroup();
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }
        public void Cancel() {
        }
        public void Finish() {
            if (SelectedColor == null) return;
            DocManager.Inst.StartUndoGroup();

            if (index == -1) {
                if (remark != null && Text != null) {
                    string? color = SelectedColor.ToString();
                    if (color == null) return;
                    remark.updateRemark(Text, color, this.postion);
                    DocManager.Inst.ExecuteCmd(new AddRemarkCommand(part, remark));
                }
            } else {
                if (Text != null) {
                    string? color = SelectedColor.ToString();
                    if (color == null) return;
                    var mark = new URemark(Text, color, this.postion);
                    DocManager.Inst.ExecuteCmd(new ChangeRemarkCommand(part, mark, index));
                }
            }
            
            DocManager.Inst.EndUndoGroup();
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }
    }
}
