using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.ViewModels {
    public class NoteSelectionViewModel {
        public NoteSelectionViewModel() {
            Notes = new SortedSet<UNote>();
        }
        public NoteSelectionViewModel(UVoicePart part) {
            Notes = new SortedSet<UNote>(part.notes);
            Head = Notes.LastOrDefault();
        }
        public NoteSelectionViewModel(IEnumerable<UNote> notes) {
            this.Notes = new SortedSet<UNote>(notes);
            // default to insert/change selection at end
            Head = Notes.LastOrDefault();
        }
        public NoteSelectionViewModel(UNote note) {
            this.Notes = new SortedSet<UNote>(new UNote[] { note });
            Head = note;
        }

        // REVIEW should this be private?
        /// <summary>
        /// Actual selected notes. Stored as sorted set to always have correct first/last
        /// </summary>
        public SortedSet<UNote> Notes;
        /// <summary>
        /// The selection move/change point (either first/last of selection)
        /// </summary>
        public UNote? Head;
        
        public int Count => Notes.Count;
        public bool IsEmpty => Notes.Count == 0;
        private bool IsMultiple => Notes.Count > 1;
        /// <summary>
        /// true if the range is being resized from left side of selection rather than right
        /// </summary>
        private bool IsReversed => IsMultiple && Head == Notes.FirstOrDefault() ? true : false;
        public UNote? First() { return Notes.FirstOrDefault(); }
        public UNote? Last() { return Notes.LastOrDefault(); }
        public bool Contains(UNote note) {
            return Notes.Contains(note);
        }
        /// <summary>
        /// Add note to selection
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public bool Add(UNote note) {
            var wasAdded = Notes.Add(note);
            if (wasAdded) {
                Head = note;
            }
            return wasAdded;
        }
        public bool Add(IEnumerable<UNote> notes) {
            bool wasChange = false;
            lock (Notes) {
                var initialLast = Notes.LastOrDefault();
                foreach (var note in notes) {
                    if (Notes.Add(note)) {
                        wasChange = true;
                    }
                }
                if (wasChange) {
                    // check if added were put in the front or in the back
                    // NOTE this relies on _notes being sortedlist
                    Head = initialLast == Notes.LastOrDefault()
                        ? Notes.FirstOrDefault()
                        : Notes.LastOrDefault();
                }
            }
            return wasChange;
        }
        public bool Remove(UNote note) {
            // if head is at beginning of selection or end
            var isReversed = IsReversed;
            var wasRemoved = Notes.Remove(note);
            if (wasRemoved) {
                // try to keep original add/remove direction.
                // will set to null if empty
                Head = isReversed ? Notes.FirstOrDefault() : Notes.LastOrDefault();
            }
            return wasRemoved;
        }
        public bool Remove(IEnumerable<UNote> notes) {
            // if head is at beginning of selection or end
            var isReversed = IsReversed;
            bool wasChange = false;
            lock (Notes) {
                foreach (var note in notes) {
                    wasChange |= Notes.Remove(note);
                }
            }
            if (wasChange) {
                // try to keep original add/remove direction.
                // will set to null if empty
                Head = isReversed ? Notes.FirstOrDefault() : Notes.LastOrDefault();
            }
            return wasChange;
        }
        /// <summary>
        /// Swap out current selection with single note
        /// </summary>
        /// <param name="note"></param>
        public bool Select(UNote? note) {
            if (Notes.Count == 1 && Head == note) {
                return false;
            }
            Notes.Clear();
            if (note != null) {
                Notes.Add(note);
            }
            Head = note;
            return true;
        }
        public bool Select(UVoicePart part) {
            return Select(part.notes);
        }
        public bool Select(IEnumerable<UNote> notes) {
            Notes = new SortedSet<UNote>(notes);
            Head = Notes.LastOrDefault();
            return true;
        }
        public bool SelectAll() {
            int initialCount = Notes.Count;
            lock (Notes) {
                var cursor = Notes.FirstOrDefault();
                while ((cursor = cursor?.Prev) != null) {
                    Notes.Add(cursor);
                }
                cursor = Notes.LastOrDefault();
                while ((cursor = cursor?.Next) != null) {
                    Notes.Add(cursor);
                }
                return Notes.Count != initialCount;
            }
        }
        public bool SelectNone() {
            var ret = IsEmpty ? false : true;
            Notes.Clear();
            Head = null;
            return ret;
        }
        public bool SelectToStart() {
            bool wasChange = false;
            lock (Notes) {
                var cursor = Notes.FirstOrDefault();
                while ((cursor = cursor?.Prev) != null) {
                    wasChange |= Notes.Add(cursor);
                }
                if (wasChange) {
                    Head = Notes.FirstOrDefault();
                }
            }
            return wasChange;
        }
        public bool SelectToEnd() {
            bool wasChange = false;
            lock (Notes) {
                var cursor = Notes.LastOrDefault();
                while ((cursor = cursor?.Next) != null) {
                    wasChange |= Notes.Add(cursor);
                }
                if (wasChange) {
                    Head = Notes.LastOrDefault();
                }
            }
            return wasChange;
        }
        /// <summary>
        /// Move selection over one, collapsing if multiple selection
        /// </summary>
        /// <param name="delta">1 for Next, -1 for Prev</param>
        /// <returns></returns>
        public bool Move(int delta) {
            if (IsEmpty) {
                return false;
            }
            var isReversed = IsReversed;
            int movesRemaining = Math.Abs(delta);
            bool wasChange = false;
            UNote? cursor = Head;
            while (
                // reduce # of moves, check if done
                movesRemaining-- > 0 &&
                // move to next/prev node and make sure can keep going
                (cursor = isReversed ? cursor?.Prev : cursor?.Next) != null
            ) {
                Head = cursor;
                wasChange = true;
            }

            // clear out current selection and select head (collapses multiple selection)
            if (wasChange || IsMultiple) {
                Select(Head!);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Expand/shrink selection in specified delta direction
        /// </summary>
        /// <param name="delta">Move selection "head" left (-1) or right (1)</param>
        /// <returns></returns>
        public bool Resize(int delta) {
            if (IsEmpty || delta == 0) {
                return false;
            }
            // doubt this guard is necessary, as head should always be set if !isempty
            if (Head == null) {
                Head = Notes.Last();
            }

            int movesRemaining = Math.Abs(delta);
            bool isForwardMove = delta > 0;
            bool wasChange = false;

            lock (Notes) {
                UNote? cursor = Head;
                while (
                    // reduce # of moves, check if done
                    movesRemaining-- > 0 &&
                    // move to next/prev node and make sure can keep going
                    (cursor = isForwardMove ? cursor?.Next : cursor?.Prev) != null
                ) {
                    // check if delta will make smaller
                    // note single selection = false - alway expand rather than shrink
                    bool isShrink = IsMultiple && (IsReversed ? isForwardMove : !isForwardMove);

                    if (isShrink) {
                        wasChange |= Notes.Remove(Head);
                    } else {
                        wasChange |= Notes.Add(cursor);
                    }
                    Head = cursor;
                }
            }

            // COMBAK this will always be true, right?
            return wasChange;
        }
        public bool MovePrev() {
            return Move(-1);
        }
        public bool MoveNext() {
            return Move(1);
        }
        public bool Expand(int delta = 1) {
            return Resize(IsReversed ? -delta : delta);
        }
        public bool Shrink(int delta = 1) {
            if (Notes.Count - delta <= 0) {
                return Select(Head);
            }
            return Resize(IsReversed ? delta : -delta);
        }
        /// <summary>
        /// If multiple selection shrink to 1, otherwise clear
        /// </summary>
        /// <returns></returns>
        public bool Cancel() {
            switch (Notes.Count) {
                case 0:
                    return false;
                case 1:
                    SelectNone();
                    return true;
                default:
                    var singleNote = Head ?? Notes.First();
                    Select(singleNote);
                    return true;
            }
        }
        public List<UNote> ToList() {
            return Notes.ToList();
        }
        public UNote[] ToArray() {
            return Notes.ToArray();
        }
    }
}
