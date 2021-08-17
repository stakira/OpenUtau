using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI {
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class MidiWindow : BorderlessWindow, ICmdSubscriber {
        readonly MidiViewModel midiVM;
        readonly MidiViewHitTest midiHT;
        ContextMenu pitchCxtMenu;

        RoutedEventHandler pitchShapeDelegate;
        class PitchPointHitTestResultContainer { public PitchPointHitInfo Result; }
        PitchPointHitTestResultContainer pitHitContainer;

        public MidiWindow() {
            InitializeComponent();

            this.CloseButtonClicked += (o, e) => { Hide(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.NoteMaxHeight;
            viewScaler.Min = UIConstants.NoteMinHeight;
            viewScaler.Value = UIConstants.NoteDefaultHeight;
            viewScaler.ViewScaled += viewScaler_ViewScaled;

            midiVM = (MidiViewModel)this.Resources["midiVM"];
            midiVM.TimelineCanvas = this.timelineCanvas;
            midiVM.MidiCanvas = this.notesCanvas;
            midiVM.PhonemeCanvas = this.phonemeCanvas;
            midiVM.ExpCanvas = this.expCanvas;
            DocManager.Inst.AddSubscriber(midiVM);

            midiHT = new MidiViewHitTest(midiVM);

            List<ExpComboBoxViewModel> comboVMs = new List<ExpComboBoxViewModel>()
            {
                new ExpComboBoxViewModel() { Index=0 },
                new ExpComboBoxViewModel() { Index=1 },
                new ExpComboBoxViewModel() { Index=2 },
                new ExpComboBoxViewModel() { Index=3 }
            };

            comboVMs[0].CreateBindings(expCombo0);
            comboVMs[1].CreateBindings(expCombo1);
            comboVMs[2].CreateBindings(expCombo2);
            comboVMs[3].CreateBindings(expCombo3);

            InitPitchPointContextMenu();

            DocManager.Inst.AddSubscriber(this);
        }

        protected override void OnDeactivated(EventArgs e) {
            base.OnDeactivated(e);
            LyricBox.EndNoteEditing();
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            DocManager.Inst.RemoveSubscriber(midiVM);
        }

        void InitPitchPointContextMenu() {
            pitchCxtMenu = new ContextMenu {
                Background = Brushes.White
            };
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease In/Out" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Linear" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease In" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease Out" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Snap to Previous Note" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Delete Point" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Add Point" });

            pitHitContainer = new PitchPointHitTestResultContainer();
            pitchShapeDelegate = (_o, _e) => {
                var o = _o as MenuItem;
                var pitHit = pitHitContainer.Result;
                if (o == pitchCxtMenu.Items[4]) {
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new SnapPitchPointCommand(pitHit.Note));
                    DocManager.Inst.EndUndoGroup();
                } else if (o == pitchCxtMenu.Items[5]) {
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(midiVM.Part, pitHit.Note, pitHit.Index));
                    DocManager.Inst.EndUndoGroup();
                } else if (o == pitchCxtMenu.Items[6]) {
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new AddPitchPointCommand(pitHit.Note, new PitchPoint(pitHit.X, pitHit.Y), pitHit.Index + 1));
                    DocManager.Inst.EndUndoGroup();
                } else {
                    PitchPointShape shape =
                        o == pitchCxtMenu.Items[0] ? PitchPointShape.io :
                        o == pitchCxtMenu.Items[2] ? PitchPointShape.i :
                        o == pitchCxtMenu.Items[3] ? PitchPointShape.o : PitchPointShape.l;
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(pitHit.Note.pitch.data[pitHit.Index], shape));
                    DocManager.Inst.EndUndoGroup();
                }
            };

            foreach (var item in pitchCxtMenu.Items) {
                if (item is MenuItem _item) {
                    _item.Click += pitchShapeDelegate;
                }
            }
        }

        void viewScaler_ViewScaled(object sender, EventArgs e) {
            double zoomCenter = (midiVM.OffsetY + midiVM.ViewHeight / 2) / midiVM.TrackHeight;
            midiVM.TrackHeight = ((ViewScaledEventArgs)e).Value;
            midiVM.OffsetY = midiVM.TrackHeight * zoomCenter - midiVM.ViewHeight / 2;
            midiVM.MarkUpdate();
        }

        private TimeSpan lastFrame = TimeSpan.Zero;

        void RenderLoop(object sender, EventArgs e) {
            if (midiVM.Part == null || midiVM.Project == null) return;

            TimeSpan nextFrame = ((RenderingEventArgs)e).RenderingTime;
            double deltaTime = (nextFrame - lastFrame).TotalMilliseconds;
            lastFrame = nextFrame;

            DragScroll(deltaTime);
            keyboardBackground.RenderIfUpdated();
            tickBackground.RenderIfUpdated();
            timelineBackground.RenderIfUpdated();
            keyTrackBackground.RenderIfUpdated();
            expTickBackground.RenderIfUpdated();
            midiVM.RedrawIfUpdated();
        }

        public void DragScroll(double deltaTime) {
            if (Mouse.Captured == this.notesCanvas && Mouse.LeftButton == MouseButtonState.Pressed) {

                const double scrollSpeed = 0.015;
                Point mousePos = Mouse.GetPosition(notesCanvas);
                bool needUdpate = false;
                double delta = scrollSpeed * deltaTime;
                if (mousePos.X < 0) {
                    this.horizontalScroll.Value -= this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                } else if (mousePos.X > notesCanvas.ActualWidth) {
                    this.horizontalScroll.Value += this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (mousePos.Y < 0 && Mouse.Captured == this.notesCanvas) {
                    this.verticalScroll.Value -= this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                } else if (mousePos.Y > notesCanvas.ActualHeight && Mouse.Captured == this.notesCanvas) {
                    this.verticalScroll.Value += this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (needUdpate) {
                    notesCanvas_MouseMoveInternal(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                }
            } else if (Mouse.Captured == timelineCanvas && Mouse.LeftButton == MouseButtonState.Pressed) {
                Point mousePos = Mouse.GetPosition(timelineCanvas);
                timelineCanvas_MouseMove_Helper(mousePos);
            }
        }

        # region Note Canvas

        Rectangle selectionBox;
        Nullable<Point> selectionStart;
        int _lastNoteLength = 480;

        bool _inMove = false;
        bool _inResize = false;
        UNote _noteHit;
        bool _inPitMove = false;
        PitchPoint _pitHit;
        int _pitHitIndex;
        int _tickMoveRelative;
        int _tickMoveStart;
        UNote _noteMoveNoteLeft;
        UNote _noteMoveNoteRight;
        UNote _noteMoveNoteMin;
        UNote _noteMoveNoteMax;
        UNote _noteResizeShortest;
        UNote _noteInEdit;
        bool _inVibratoEdit;
        VibratoHitInfo _vibratoHit;

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            LyricBox.EndNoteEditing();
            Point mousePos = e.GetPosition((Canvas)sender);

            var visualHit = VisualTreeHelper.HitTest(notesCanvas, mousePos).VisualHit;
            System.Diagnostics.Debug.WriteLine("Mouse hit " + visualHit.ToString());

            var pitHitResult = midiHT.HitTestPitchPoint(mousePos);
            var vbrHitResult = midiHT.HitTestVibrato(mousePos);
            if (pitHitResult != null) {
                if (pitHitResult.OnPoint) {
                    _inPitMove = true;
                    _pitHit = pitHitResult.Note.pitch.data[pitHitResult.Index];
                    _pitHitIndex = pitHitResult.Index;
                    _noteHit = pitHitResult.Note;
                    DocManager.Inst.StartUndoGroup();
                }
            } else if (vbrHitResult.hit) {
                if (vbrHitResult.hitToggle) {
                    var note = vbrHitResult.note;
                    var vibrato = note.vibrato;
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(midiVM.Part, note, vibrato.length == 0 ? 50f : 0));
                    DocManager.Inst.EndUndoGroup();
                } else {
                    _inVibratoEdit = true;
                    _vibratoHit = vbrHitResult;
                    DocManager.Inst.StartUndoGroup();
                }
            } else {
                NoteHitInfo hit = midiHT.HitTestNote(mousePos);
                if (hit.note != null && hit.hitBody) {
                    System.Diagnostics.Debug.WriteLine("Mouse hit" + hit.note.ToString());
                }

                if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
                    selectionStart = new Point(midiVM.CanvasToQuarter(mousePos.X), midiVM.CanvasToNoteNum(mousePos.Y));

                    if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) midiVM.DeselectAll();

                    if (selectionBox == null) {
                        selectionBox = new Rectangle() {
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Fill = ThemeManager.BarNumberBrush,
                            Width = 0,
                            Height = 0,
                            Opacity = 0.5,
                            RadiusX = 8,
                            RadiusY = 8,
                            IsHitTestVisible = false
                        };
                        notesCanvas.Children.Add(selectionBox);
                        Panel.SetZIndex(selectionBox, 1000);
                        selectionBox.Visibility = Visibility.Visible;
                    } else {
                        selectionBox.Width = 0;
                        selectionBox.Height = 0;
                        Panel.SetZIndex(selectionBox, 1000);
                        selectionBox.Visibility = Visibility.Visible;
                    }
                    Mouse.OverrideCursor = Cursors.Cross;
                } else {
                    if (hit.hitBody) {
                        _noteHit = hit.note;
                        if (!midiVM.SelectedNotes.Contains(hit.note)) {
                            midiVM.DeselectAll();
                        }

                        if (e.ClickCount == 2) {
                            _noteInEdit = _noteHit;
                            LyricBox.Show(midiVM.Part, _noteInEdit, _noteInEdit.lyric);
                        } else if (!hit.hitResizeArea) {
                            // Move note
                            _inMove = true;
                            _tickMoveRelative = midiVM.CanvasToSnappedTick(mousePos.X) - hit.note.position;
                            _tickMoveStart = hit.note.position;
                            _lastNoteLength = hit.note.duration;
                            if (midiVM.SelectedNotes.Count != 0) {
                                _noteMoveNoteMax = _noteMoveNoteMin = hit.note;
                                _noteMoveNoteLeft = _noteMoveNoteRight = hit.note;
                                foreach (UNote note in midiVM.SelectedNotes) {
                                    if (note.position < _noteMoveNoteLeft.position) _noteMoveNoteLeft = note;
                                    if (note.End > _noteMoveNoteRight.End) _noteMoveNoteRight = note;
                                    if (note.noteNum < _noteMoveNoteMin.noteNum) _noteMoveNoteMin = note;
                                    if (note.noteNum > _noteMoveNoteMax.noteNum) _noteMoveNoteMax = note;
                                }
                            }
                            DocManager.Inst.StartUndoGroup();
                        } else {
                            // Resize note
                            _inResize = true;
                            Mouse.OverrideCursor = Cursors.SizeWE;
                            if (midiVM.SelectedNotes.Count != 0) {
                                _noteResizeShortest = hit.note;
                                foreach (UNote note in midiVM.SelectedNotes)
                                    if (note.duration < _noteResizeShortest.duration) _noteResizeShortest = note;
                            }
                            DocManager.Inst.StartUndoGroup();
                        }
                    } else // Add note
                      {
                        UNote newNote = DocManager.Inst.Project.CreateNote(
                            midiVM.CanvasToNoteNum(mousePos.Y),
                            midiVM.CanvasToSnappedTick(mousePos.X),
                            _lastNoteLength);

                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new AddNoteCommand(midiVM.Part, newNote));
                        DocManager.Inst.EndUndoGroup();
                        midiVM.MarkUpdate();
                        // Enable drag
                        midiVM.DeselectAll();
                        _inMove = true;
                        _noteHit = newNote;
                        _tickMoveRelative = 0;
                        _tickMoveStart = newNote.position;
                        DocManager.Inst.StartUndoGroup();
                    }
                }
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            _inMove = false;
            _inResize = false;
            _noteHit = null;
            _inPitMove = false;
            _pitHit = null;
            _inVibratoEdit = false;
            _vibratoHit = default;
            DocManager.Inst.EndUndoGroup();
            // End selection
            selectionStart = null;
            if (selectionBox != null) {
                Panel.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = Visibility.Hidden;
            }
            midiVM.DoneTempSelect();
            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e) {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMoveInternal(mousePos);
        }

        private void notesCanvas_MouseMoveInternal(Point mousePos) {
            if (midiVM.Part == null) return;
            if (selectionStart != null) { // Selection
                double top = midiVM.NoteNumToCanvas(Math.Max(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y));
                double bottom = midiVM.NoteNumToCanvas(Math.Min(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y) - 1);
                double left = Math.Min(mousePos.X, midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Width = Math.Abs(mousePos.X - midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, left);
                Canvas.SetTop(selectionBox, top);
                midiVM.TempSelectInBox(selectionStart.Value.X, midiVM.CanvasToQuarter(mousePos.X), (int)selectionStart.Value.Y, midiVM.CanvasToNoteNum(mousePos.Y));
            } else if (_inPitMove) {
                double tickX = midiVM.CanvasToQuarter(mousePos.X) * DocManager.Inst.Project.resolution - _noteHit.position;
                double deltaX = DocManager.Inst.Project.TickToMillisecond(tickX) - _pitHit.X;
                if (_pitHitIndex != 0) deltaX = Math.Max(deltaX, _noteHit.pitch.data[_pitHitIndex - 1].X - _pitHit.X);
                if (_pitHitIndex != _noteHit.pitch.data.Count - 1) deltaX = Math.Min(deltaX, _noteHit.pitch.data[_pitHitIndex + 1].X - _pitHit.X);
                double deltaY = Keyboard.Modifiers == ModifierKeys.Shift ? Math.Round(midiVM.CanvasToPitch(mousePos.Y) - _noteHit.noteNum) * 10 - _pitHit.Y :
                    (midiVM.CanvasToPitch(mousePos.Y) - _noteHit.noteNum) * 10 - _pitHit.Y;
                if (_noteHit.pitch.data.First() == _pitHit && _noteHit.pitch.snapFirst || _noteHit.pitch.data.Last() == _pitHit) deltaY = 0;
                if (deltaX != 0 || deltaY != 0)
                    DocManager.Inst.ExecuteCmd(new MovePitchPointCommand(_pitHit, (float)deltaX, (float)deltaY));
            } else if (_inVibratoEdit) {
                var project = DocManager.Inst.Project;
                var note = _vibratoHit.note;
                float vibratoTick = note.vibrato.length / 100f * note.duration;
                float startTick = note.position + note.duration - vibratoTick;
                if (_vibratoHit.hitStart) {
                    int tick = midiVM.CanvasToTick(mousePos.X);
                    float newLength = 100f - 100f * (tick - note.position) / note.duration;
                    if (newLength != note.vibrato.length) {
                        DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(midiVM.Part, note, newLength));
                    }
                } else if (_vibratoHit.hitIn) {
                    int tick = midiVM.CanvasToTick(mousePos.X);
                    float newIn = (tick - startTick) / vibratoTick * 100f;
                    if (newIn != note.vibrato.@in) {
                        DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(midiVM.Part, note, newIn));
                    }
                } else if (_vibratoHit.hitOut) {
                    int tick = midiVM.CanvasToTick(mousePos.X);
                    float newOut = (note.position + note.duration - tick) / vibratoTick * 100f;
                    if (newOut != note.vibrato.@out) {
                        DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(midiVM.Part, note, newOut));
                    }
                } else if (_vibratoHit.hitDepth) {
                    float tone = midiVM.CanvasToTone(mousePos.Y);
                    float newDepth = note.vibrato.ToneToDepth(note, tone);
                    if (newDepth != note.vibrato.depth) {
                        DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(midiVM.Part, note, newDepth));
                    }
                } else if (_vibratoHit.hitPeriod) {
                    float periodTick = project.MillisecondToTick(note.vibrato.period);
                    float shiftTick = periodTick * note.vibrato.shift / 100f;
                    float tick = midiVM.CanvasToTick(mousePos.X) - startTick - shiftTick;
                    float newPeriod = (float)DocManager.Inst.Project.TickToMillisecond(tick);
                    if (newPeriod != note.vibrato.depth) {
                        DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(midiVM.Part, note, newPeriod));
                    }
                } else if (_vibratoHit.hitShift) {
                    float periodTick = project.MillisecondToTick(note.vibrato.period);
                    float deltaTick = midiVM.CanvasToTick(mousePos.X) - midiVM.CanvasToTick(_vibratoHit.point.X);
                    float deltaShift = deltaTick / periodTick * 100f;
                    float newShift = _vibratoHit.initialShift + deltaShift;
                    if (newShift != note.vibrato.depth) {
                        DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(midiVM.Part, note, newShift));
                    }
                }
            } else if (_inMove) { // Move Note
                if (midiVM.SelectedNotes.Count == 0) {
                    int newNoteNum = Math.Max(0, Math.Min(UIConstants.MaxNoteNum - 1, midiVM.CanvasToNoteNum(mousePos.Y)));
                    int newPosTick = Math.Max(0, Math.Min((int)(midiVM.QuarterCount * midiVM.Project.resolution) - _noteHit.duration,
                        (int)(midiVM.Project.resolution * midiVM.CanvasToSnappedQuarter(mousePos.X)) - _tickMoveRelative));
                    if (newNoteNum != _noteHit.noteNum || newPosTick != _noteHit.position)
                        DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, _noteHit, newPosTick - _noteHit.position, newNoteNum - _noteHit.noteNum));
                } else {
                    int deltaNoteNum = midiVM.CanvasToNoteNum(mousePos.Y) - _noteHit.noteNum;
                    int deltaPosTick = ((int)(midiVM.Project.resolution * midiVM.CanvasToSnappedQuarter(mousePos.X)) - _tickMoveRelative) - _noteHit.position;

                    if (deltaNoteNum != 0 || deltaPosTick != 0) {
                        bool changeNoteNum = deltaNoteNum + _noteMoveNoteMin.noteNum >= 0 && deltaNoteNum + _noteMoveNoteMax.noteNum < UIConstants.MaxNoteNum;
                        bool changePosTick = deltaPosTick + _noteMoveNoteLeft.position >= 0 && deltaPosTick + _noteMoveNoteRight.End <= midiVM.QuarterCount * midiVM.Project.resolution;
                        if (changeNoteNum || changePosTick)

                            DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, midiVM.SelectedNotes,
                                    changePosTick ? deltaPosTick : 0, changeNoteNum ? deltaNoteNum : 0));
                    }
                }
                Mouse.OverrideCursor = Cursors.SizeAll;
            } else if (_inResize) { // resize
                if (midiVM.SelectedNotes.Count == 0) {
                    int newDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.resolution) - _noteHit.position;
                    if (newDurTick != _noteHit.duration && newDurTick >= midiVM.GetSnapUnit() * midiVM.Project.resolution) {
                        DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, _noteHit, newDurTick - _noteHit.duration));
                        _lastNoteLength = newDurTick;
                    }
                } else {
                    int deltaDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.resolution) - _noteHit.End;
                    if (deltaDurTick != 0 && deltaDurTick + _noteResizeShortest.duration > midiVM.GetSnapUnit()) {
                        DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, midiVM.SelectedNotes, deltaDurTick));
                        _lastNoteLength = _noteHit.duration;
                    }
                }
            } else if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
              {
                NoteHitInfo hit = midiHT.HitTestNote(mousePos);
                if (hit.hitBody) {
                    DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, hit.note));
                }
            } else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released) {
                var pitHit = midiHT.HitTestPitchPoint(mousePos);
                var vbrHit = midiHT.HitTestVibrato(mousePos);
                if (pitHit != null) {
                    Mouse.OverrideCursor = Cursors.Hand;
                } else if (vbrHit.hit) {
                    if (vbrHit.hitDepth) {
                        Mouse.OverrideCursor = Cursors.SizeNS;
                    } else if (vbrHit.hitPeriod) {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    } else {
                        Mouse.OverrideCursor = Cursors.Hand;
                    }
                } else {
                    NoteHitInfo hit = midiHT.HitTestNote(mousePos);
                    if (hit.hitResizeArea) {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    } else if (hit.hitX) {
                        Mouse.OverrideCursor = null;
                    } else {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            LyricBox.EndNoteEditing();
            Point mousePos = e.GetPosition((Canvas)sender);

            var pitHit = midiHT.HitTestPitchPoint(mousePos);
            if (pitHit != null) {
                Mouse.OverrideCursor = null;
                pitHitContainer.Result = pitHit;

                if (pitHit.OnPoint) {
                    ((MenuItem)pitchCxtMenu.Items[4]).Header = pitHit.Note.pitch.snapFirst ? "Unsnap from previous point" : "Snap to previous point";
                    ((MenuItem)pitchCxtMenu.Items[4]).Visibility = pitHit.Index == 0 ? Visibility.Visible : Visibility.Collapsed;

                    if (pitHit.Index == 0 || pitHit.Index == pitHit.Note.pitch.data.Count - 1) ((MenuItem)pitchCxtMenu.Items[5]).Visibility = Visibility.Collapsed;
                    else ((MenuItem)pitchCxtMenu.Items[5]).Visibility = Visibility.Visible;

                    ((MenuItem)pitchCxtMenu.Items[6]).Visibility = Visibility.Collapsed;
                } else {
                    ((MenuItem)pitchCxtMenu.Items[4]).Visibility = Visibility.Collapsed;
                    ((MenuItem)pitchCxtMenu.Items[5]).Visibility = Visibility.Collapsed;
                    ((MenuItem)pitchCxtMenu.Items[6]).Visibility = Visibility.Visible;
                }

                pitchCxtMenu.IsOpen = true;
                pitchCxtMenu.PlacementTarget = this.notesCanvas;
            } else {
                NoteHitInfo hit = midiHT.HitTestNote(mousePos);
                DocManager.Inst.StartUndoGroup();
                if (hit.hitBody && (midiVM.SelectedNotes.Count == 0 || midiVM.SelectedNotes.Contains(hit.note))) {
                    DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, hit.note));
                } else {
                    midiVM.DeselectAll();
                }

                ((UIElement)sender).CaptureMouse();
                Mouse.OverrideCursor = Cursors.No;
            }
            System.Diagnostics.Debug.WriteLine("Total notes: " + midiVM.Part.notes.Count + " selected: " + midiVM.SelectedNotes.Count);
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            Mouse.OverrideCursor = null;
            ((UIElement)sender).ReleaseMouseCapture();
            DocManager.Inst.EndUndoGroup();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                timelineCanvas_MouseWheel(sender, e);
            } else if (Keyboard.Modifiers == ModifierKeys.Shift) {
                midiVM.OffsetX -= midiVM.ViewWidth * 0.001 * e.Delta;
            } else if (Keyboard.Modifiers == ModifierKeys.Alt) {
            } else {
                verticalScroll.Value -= verticalScroll.SmallChange * e.Delta / 100;
                verticalScroll.Value = Math.Max(verticalScroll.Minimum, Math.Min(verticalScroll.Maximum, verticalScroll.Value));
            }
        }

        #endregion

        #region Navigate Drag

        private void navigateDrag_NavDrag(object sender, EventArgs e) {
            midiVM.OffsetX += ((NavDragEventArgs)e).X * midiVM.SmallChangeX;
            midiVM.OffsetY += ((NavDragEventArgs)e).Y * midiVM.SmallChangeY * 0.5;
            midiVM.MarkUpdate();
        }

        #endregion

        #region Timeline Canvas

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            const double zoomSpeed = 0.0012;
            Point mousePos = e.GetPosition((UIElement)sender);
            double zoomCenter;
            if (midiVM.OffsetX == 0 && mousePos.X < 128) zoomCenter = 0;
            else zoomCenter = (midiVM.OffsetX + mousePos.X) / midiVM.QuarterWidth;
            midiVM.QuarterWidth *= 1 + e.Delta * zoomSpeed;
            midiVM.OffsetX = Math.Max(0, Math.Min(midiVM.TotalWidth, zoomCenter * midiVM.QuarterWidth - mousePos.X));
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            Point mousePos = e.GetPosition((UIElement)sender);
            int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.resolution);
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            ((Canvas)sender).CaptureMouse();
        }

        private void timelineCanvas_MouseMove(object sender, MouseEventArgs e) {
        }

        private void timelineCanvas_MouseMove_Helper(Point mousePos) {
            if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.Captured == timelineCanvas) {
                int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.resolution);
                if (midiVM.playPosTick != tick + midiVM.Part.PosTick)
                    DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            }
        }

        private void timelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        #endregion

        #region Keys Action

        // TODO : keys mouse over, click, release, click and move

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        }

        private void keysCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        }

        private void keysCanvas_MouseMove(object sender, MouseEventArgs e) {
        }

        private void keysCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            //this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        #endregion

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (LyricBox.IsVisible) {
                // Prevents window from handling events, so that events can be handled by text box default behaviour.
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) {
                this.Hide();
            } else if (midiVM.Part == null) {
                return;
            } else if (Keyboard.Modifiers == ModifierKeys.Control) // Ctrl
              {
                if (e.Key == Key.A) {
                    midiVM.SelectAll();
                } else if (e.Key == Key.Z) {
                    midiVM.DeselectAll();
                    DocManager.Inst.Undo();
                } else if (e.Key == Key.Y) {
                    midiVM.DeselectAll();
                    DocManager.Inst.Redo();
                }
            } else if (Keyboard.Modifiers == 0) // No midifiers
              {
                if (e.Key == Key.Delete) {
                    if (midiVM.SelectedNotes.Count > 0) {
                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, midiVM.SelectedNotes));
                        DocManager.Inst.EndUndoGroup();
                    }
                } else if (e.Key == Key.I) {
                    midiVM.ShowPitch = !midiVM.ShowPitch;
                } else if (e.Key == Key.O) {
                    midiVM.ShowPhoneme = !midiVM.ShowPhoneme;
                } else if (e.Key == Key.P) {
                    midiVM.Snap = !midiVM.Snap;
                }
            }
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            this.Hide();
        }

        private void expCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            ((Canvas)sender).CaptureMouse();
            DocManager.Inst.StartUndoGroup();
            Point mousePos = e.GetPosition((UIElement)sender);
            expCanvas_SetExpHelper(mousePos);
        }

        private void expCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            DocManager.Inst.EndUndoGroup();
            ((Canvas)sender).ReleaseMouseCapture();
        }

        private void expCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed) {
                Point mousePos = e.GetPosition((UIElement)sender);
                expCanvas_SetExpHelper(mousePos);
            }
        }

        private void expCanvas_SetExpHelper(Point mousePos) {
            if (midiVM.Part == null) return;
            float newValue;
            string _key = midiVM.visibleExpElement.Key;
            var _expDef = DocManager.Inst.Project.expressions[_key];
            if (Keyboard.Modifiers == ModifierKeys.Alt) {
                newValue = _expDef.defaultValue;
            } else {
                newValue = (float)Math.Max(_expDef.min, Math.Min(_expDef.max, (1 - mousePos.Y / expCanvas.ActualHeight) * (_expDef.max - _expDef.min) + _expDef.min));
            }
            NoteHitInfo hit = midiHT.HitTestNote(mousePos);
            if (!hit.hitX) {
                return;
            }
            if (midiVM.SelectedNotes.Count == 0 || midiVM.SelectedNotes.Contains(hit.note)) {
                DocManager.Inst.ExecuteCmd(new SetUExpressionCommand(midiVM.Part, hit.note, midiVM.visibleExpElement.Key, newValue));
            }
        }

        private void mainButton_Click(object sender, RoutedEventArgs e) {
            DocManager.Inst.ExecuteCmd(new ShowPitchExpNotification());
        }

        void ICmdSubscriber.OnNext(UCommand cmd, bool isUndo) {
            switch (cmd) {
                case WillRemoveTrackNotification _:
                    Hide();
                    break;
                default:
                    break;
            }
        }
    }
}
