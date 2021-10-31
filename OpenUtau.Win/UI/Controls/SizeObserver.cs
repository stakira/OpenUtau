using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenUtau.UI.Controls
{
    public static class SizeObserver
    {
        public static readonly DependencyProperty ObserveProperty = DependencyProperty.RegisterAttached(
            "Observe",
            typeof(bool),
            typeof(SizeObserver),
            new FrameworkPropertyMetadata(OnObserveChanged));

        public static readonly DependencyProperty ObservedWidthProperty = DependencyProperty.RegisterAttached(
            "ObservedWidth",
            typeof(double),
            typeof(SizeObserver));

        public static readonly DependencyProperty ObservedHeightProperty = DependencyProperty.RegisterAttached(
            "ObservedHeight",
            typeof(double),
            typeof(SizeObserver));

        public static bool GetObserve(FrameworkElement frameworkElement)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            return (bool)frameworkElement.GetValue(ObserveProperty);
        }

        public static void SetObserve(FrameworkElement frameworkElement, bool observe)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            frameworkElement.SetValue(ObserveProperty, observe);
        }

        public static double GetObservedWidth(FrameworkElement frameworkElement)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            return (double)frameworkElement.GetValue(ObservedWidthProperty);
        }

        public static void SetObservedWidth(FrameworkElement frameworkElement, double observedWidth)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            frameworkElement.SetValue(ObservedWidthProperty, observedWidth);
        }

        public static double GetObservedHeight(FrameworkElement frameworkElement)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            return (double)frameworkElement.GetValue(ObservedHeightProperty);
        }

        public static void SetObservedHeight(FrameworkElement frameworkElement, double observedHeight)
        {
            if (frameworkElement == null) throw new Exception("frameworkElement is null");
            frameworkElement.SetValue(ObservedHeightProperty, observedHeight);
        }

        private static void OnObserveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = (FrameworkElement)dependencyObject;

            if ((bool)e.NewValue)
            {
                frameworkElement.SizeChanged += OnFrameworkElementSizeChanged;
                UpdateObservedSizesForFrameworkElement(frameworkElement);
            }
            else
            {
                frameworkElement.SizeChanged -= OnFrameworkElementSizeChanged;
            }
        }

        private static void OnFrameworkElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateObservedSizesForFrameworkElement((FrameworkElement)sender);
        }

        private static void UpdateObservedSizesForFrameworkElement(FrameworkElement frameworkElement)
        {
            // WPF 4.0 onwards
            frameworkElement.SetCurrentValue(ObservedWidthProperty, frameworkElement.ActualWidth);
            frameworkElement.SetCurrentValue(ObservedHeightProperty, frameworkElement.ActualHeight);

            // WPF 3.5 and prior
            ////SetObservedWidth(frameworkElement, frameworkElement.ActualWidth);
            ////SetObservedHeight(frameworkElement, frameworkElement.ActualHeight);
        }
    }
}
