using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;

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
        private bool restoreIfMove = false;

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
        
        public FrameworkElement MenuContent
        {
            set { SetValue(MenuContentProperty, value); }
            get { return (FrameworkElement)GetValue(MenuContentProperty); }
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

            var titleBar = GetTemplateChild(PART_TitleBar) as Border;
            titleBar.MouseLeftButtonDown += titleBar_MouseLeftButtonDown;
            titleBar.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) { restoreIfMove = false; };
            titleBar.MouseMove += titleBar_MouseMove;
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

        private void titleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
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
            else if (WindowState != System.Windows.WindowState.Maximized)
            {
                DragMove();
                // The 'correct' way to make a borderless window movable
                // http://stackoverflow.com/questions/3274097/way-to-make-a-windowless-wpf-window-draggable-without-getting-invalidoperationex/3275712#3275712
            }
            else
            {
                restoreIfMove = true;
            }
        }

        private void titleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (restoreIfMove)
            {
                restoreIfMove = false;
                double maximizedWidth = this.ActualWidth;
                double mouseX = e.GetPosition(this).X;
                double width = RestoreBounds.Width;
                double x = PointToScreen(new Point(0, 0)).X + mouseX * (1.0 - width / maximizedWidth);

                WindowState = WindowState.Normal;
                Left = x;
                Top = 0;
                DragMove();
            }
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
