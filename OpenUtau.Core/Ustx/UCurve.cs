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
                double height = PerpendicularDistance(
                    xs[first], ys[first], xs[last], ys[last], xs[index], ys[index]);
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

        private double PerpendicularDistance(int x, int y, int x1, int y1, int x2, int y2) {
            double area = 0.5 * Math.Abs(x1 * (y2 - y) + x2 * (y - y1) + x * (y1 - y2));
            double bottom = Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
            return area / bottom * 2;
        }
    }
}
