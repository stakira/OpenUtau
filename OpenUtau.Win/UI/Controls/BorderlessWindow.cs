using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Shell;

using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    [TemplatePart(Name = PART_WindowBody, Type = typeof(UIElement))]
    [TemplatePart(Name = PART_TitleBar, Type = typeof(UIElement))]
    [TemplatePart(Name = PART_Titlelabel, Type = typeof(UIElement))]
    [TemplatePart(Name = PART_MenuContent, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_MinButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_MaxButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_Content, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_WindowBorder, Type = typeof(UIElement))]
    public class BorderlessWindow : Window
    {
        public event EventHandler CloseButtonClicked;

        private const string PART_WindowBody = "PART_WindowBody";
        private const string PART_TitleBar = "PART_TitleBar";
        private const string PART_Titlelabel = "PART_Titlelabel";
        private const string PART_MenuContent = "PART_MenuContent";
        private const string PART_MinButton = "PART_MinButton";
        private const string PART_MaxButton = "PART_MaxButton";
        private const string PART_CloseButton = "PART_CloseButton";
        private const string PART_Content = "PART_Content";
        private const string PART_WindowBorder = "PART_WindowBorder";

        public static readonly DependencyProperty MenuContentProperty = DependencyProperty.Register("MenuContent", typeof(object), typeof(FrameworkElement), new UIPropertyMetadata(null));
        public static readonly DependencyProperty ResizableProperty = DependencyProperty.Register("Resizable", typeof(bool), typeof(FrameworkElement), new UIPropertyMetadata(true, ResizablePropertyChangedCallback));

        private static void ResizablePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BorderlessWindow)d).OnResizableChanged((bool)e.NewValue);
        }
        
        public FrameworkElement MenuContent
        {
            set { SetValue(MenuContentProperty, value); }
            get { return (FrameworkElement)GetValue(MenuContentProperty); }
        }
        public bool Resizable
        {
            set { SetValue(ResizableProperty, value); }
            get { return (bool)GetValue(ResizableProperty); }
        }

        private readonly WindowChrome windowChrome;

        public BorderlessWindow()
        {
            windowChrome = new WindowChrome();
            WindowChrome.SetWindowChrome(this, windowChrome);
            windowChrome.GlassFrameThickness = new Thickness(1);
            windowChrome.CornerRadius = new CornerRadius(0);
            windowChrome.CaptionHeight = 0;
            Loaded += Window_Loaded;

            // for systems below Windows 8
            var osversion = Environment.OSVersion.Version;
            if (osversion.Major == 6 && osversion.Minor < 2)
            {
                Activated += Window_Activated;
                Deactivated += Window_Deactivated;
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var border = this.GetTemplateChild(PART_WindowBorder) as Border;
            border.BorderThickness = new Thickness(1);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            var border = this.GetTemplateChild(PART_WindowBorder) as Border;
            border.BorderThickness = new Thickness(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnResizableChanged(Resizable);
        }

        private void OnResizableChanged(bool resizable)
        {
            var minButton = this.GetTemplateChild(PART_MinButton) as Button;
            var maxButton = this.GetTemplateChild(PART_MaxButton) as Button;
            if (minButton != null) minButton.Visibility = resizable ? Visibility.Visible : Visibility.Collapsed;
            if (maxButton != null) maxButton.Visibility = resizable ? Visibility.Visible : Visibility.Collapsed;
            if (windowChrome != null) windowChrome.ResizeBorderThickness = resizable ? new Thickness(4) : new Thickness(0);
        }

        static BorderlessWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BorderlessWindow), new FrameworkPropertyMetadata(typeof(BorderlessWindow)));
        }

        public override void OnApplyTemplate()
        {
            var windowBorder = GetTemplateChild(PART_WindowBorder) as Border;
            windowBorder.BorderBrush = ThemeManager.UINeutralBrushNormal;

            this.Background = ThemeManager.UIBackgroundBrushNormal;

            var minButton = GetTemplateChild(PART_MinButton) as Button;
            minButton.Click += delegate(object sender, RoutedEventArgs e) { WindowState = System.Windows.WindowState.Minimized; };

            var maxButton = GetTemplateChild(PART_MaxButton) as Button;
            maxButton.Click += maxButton_Click;

            var closeButton = GetTemplateChild(PART_CloseButton) as Button;
            closeButton.Click += closeButton_Click;
        }

        void maxButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            else
            {
                WindowState = System.Windows.WindowState.Maximized;
            }
        }

        void closeButton_Click(object sender, RoutedEventArgs e)
        {
            EventHandler handler = CloseButtonClicked;
            if (handler != null) handler(this, e);
        }
    }

    public static class CompositionTargetEx
    {
        private static TimeSpan _last = TimeSpan.Zero;

        private static event EventHandler<RenderingEventArgs> _FrameUpdating;

        public static event EventHandler<RenderingEventArgs> FrameUpdating
        {
            add
            {
                if (_FrameUpdating == null)
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                _FrameUpdating += value;
            }
            remove
            {
                _FrameUpdating -= value;
                if (_FrameUpdating == null)
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }
        }

        static void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;
            if (args.RenderingTime - _last < TimeSpan.FromMilliseconds(25))
                return;
            _last = args.RenderingTime;
            _FrameUpdating(sender, args);
        }
    }
}
