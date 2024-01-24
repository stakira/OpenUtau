using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public class UCurve {
        public const int interval = 5;

        [YamlIgnore] public UExpressionDescriptor descriptor;
        public List<int> xs = new List<int>();
        public List<int> ys = new List<int>();
        public string abbr;

        [YamlIgnore] public bool IsEmpty => xs.Count == 0 || ys.All(y => y == 0);

        public UCurve(UExpressionDescriptor descriptor) {
            Trace.Assert(descriptor != null);
            this.descriptor = descriptor;
            abbr = descriptor.abbr;
        }

        public UCurve() { }

        public UCurve(string abbr) {
            this.abbr = abbr;
        }

        public UCurve Clone() {
            return new UCurve(descriptor) {
                xs = xs.ToList(),
                ys = ys.ToList(),
            };
        }

        public bool IsEmptyBetween(int x0, int x1, int defaultValue) {
            if (Sample(x0) != defaultValue || Sample(x1) != defaultValue) {
                return false;
            }
            int idx = xs.BinarySearch(x0);
            if (idx < 0) {
                idx = ~idx;
            }
            while (idx < xs.Count && xs[idx] <= x1) {
                if (ys[idx] != defaultValue) {
                    return false;
                }
                idx++;
            }
            return true;
        }

        public int Sample(int x) {
            int idx = xs.BinarySearch(x);
            if (idx >= 0) {
                return ys[idx];
            }
            idx = ~idx;
            if (idx > 0 && idx < xs.Count) {
                return (int)Math.Round(MusicMath.Linear(xs[idx - 1], xs[idx], ys[idx - 1], ys[idx], x));
            }
            return (int)descriptor.defaultValue;
        }

        /// <summary>
        /// Samples the curve at xstart, xstart + interval, xstart + 2 * interval, ..., xstart + (count - 1) * interval.
        /// This will improve performance if the sample interval isn't 
        /// much greater than the intervals between xs in the curve, 
        /// by calling BinarySearch less often.
        /// </summary>
        /// <param name="xstart">The x value of the first sample.</param>
        /// <param name="count">The number of samples needed.</param>
        /// <param name="interval">The interval between samples.</param>
        public IEnumerable<int> Samples(int xstart, int count, int interval) {
            int[] samples = new int[count];
            if(count == 0) {
                yield break;
            }
            int idx = xs.BinarySearch(xstart);
            if (idx < 0) {
                idx = ~idx;
            }
            int x = xstart;
            for (int i = 0; i < count; i++) {
                while(idx < xs.Count && xs[idx] < x) {
                    idx++;
                }
                if(idx <xs.Count && xs[idx] == x) {
                    yield return ys[idx];
                    idx++;
                    x += interval;
                    continue;
                }
                if (idx > 0 && idx < xs.Count) {
                    yield return (int)Math.Round(MusicMath.Linear(xs[idx - 1], xs[idx], ys[idx - 1], ys[idx], x));
                } else {
                    yield return (int)descriptor.defaultValue;
                }
                x += interval;
            }
        }

        private void Insert(int x, int y) {
            int idx = xs.BinarySearch(x);
            if (idx >= 0) {
                ys[idx] = y;
                return;
            }
            idx = ~idx;
            xs.Insert(idx, x);
            ys.Insert(idx, y);
        }

        public void Set(int x, int y, int lastX, int lastY) {
            x = (int)Math.Round((float)x / interval) * interval;
            lastX = (int)Math.Round((float)lastX / interval) * interval;
            if (x == lastX) {
                int leftY = Sample(x - interval);
                int rightY = Sample(x + interval);
                Insert(x - interval, leftY);
                Insert(x, y);
                Insert(x + interval, rightY);
            } else if (x < lastX) {
                int leftY = Sample(x - interval);
                DeleteBetweenExclusive(x, lastX);
                Insert(x - interval, leftY);
                Insert(x, y);
            } else {
                int rightY = Sample(x + interval);
                DeleteBetweenExclusive(lastX, x);
                Insert(x, y);
                Insert(x + interval, rightY);
            }
        }

        /// <summary>
        /// Write multiple points in a time range into the curve, used by batch operations.
        /// </summary>
        public void SetRange(int[] x, int[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("x and y must have the same length.");
            }
            x = x.Select(v => (int)Math.Round((float)v / interval) * interval).ToArray();
            int[] toKeep = Enumerable.Range(0, x.Length - 1)
                .Where(i => x[i + 1] > x[i])
                .Append(x.Length - 1)
                .ToArray();
            x = toKeep.Select(i => x[i]).ToArray();
            y = toKeep.Select(i => y[i]).ToArray();
            if (x.Length == 0) {
                return;
            }

            int leftY = Sample(x[0] - interval);
            int rightY = Sample(x[x.Length - 1] + interval);
            DeleteBetweenExclusive(x[0] - 1, x[x.Length - 1] + 1);
            Insert(x[0] - interval, leftY);
            var idx = xs.BinarySearch(x[0]);
            if (idx < 0) {
                idx = ~idx;
            }
            xs.InsertRange(idx, x);
            ys.InsertRange(idx, y);
            Insert(x[^1] + interval, rightY);
        }

        private void DeleteBetweenExclusive(int x1, int x2) {
            int li = xs.BinarySearch(x1);
            if (li >= 0) {
                li++;
            } else {
                li = ~li;
            }
            int ri = xs.BinarySearch(x2);
            if (ri >= 0) {
                ri--;
            } else {
                ri = ~ri - 1;
            }
            if (ri >= li) {
                xs.RemoveRange(li, ri - li + 1);
                ys.RemoveRange(li, ri - li + 1);
            }
        }
        public void Simplify() {
            if (xs == null || xs.Count < 3) {
                return;
            }
            int first = 0;
            int last = xs.Count - 1;
            var toKeep = new List<int>() { first, last };
            double tolerance = Math.Min(5, (descriptor.max - descriptor.min) * 0.005);
            Simplify(first, last, tolerance, toKeep);
            toKeep.Sort();
            var newXs = new List<int>();
            var newYs = new List<int>();
            foreach (int index in toKeep) {
                newXs.Add(xs[index]);
                newYs.Add(ys[index]);
            }
            xs = newXs;
            ys = newYs;
        }

        public void Simplify(int first, int last, double tolerance, List<int> toKeep) {
            double maxHeight = 0;
            int maxHeightIdx = 0;
            for (int index = first; index < last; index++) {
                double height = Math.Abs(DeltaY(
                    xs[index], ys[index], xs[first], ys[first], xs[last], ys[last]));
                if (height > maxHeight) {
                    maxHeight = height;
                    maxHeightIdx = index;
                }
            }
            if (maxHeight > tolerance && maxHeightIdx != 0) {
                toKeep.Add(maxHeightIdx);
                Simplify(first, maxHeightIdx, tolerance, toKeep);
                Simplify(maxHeightIdx, last, tolerance, toKeep);
            }
        }

        private double DeltaY(int x, int y, int x1, int y1, int x2, int y2){
            return y - MusicMath.Linear(x1, x2, y1, y2, x);
        }
    }
}
