using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClickyWindows;

public partial class OverlayWindow : Window
{
    private DispatcherTimer? _fadeTimer;
    private Storyboard? _cursorAnimation;

    public OverlayWindow()
    {
        InitializeComponent();
        this.Loaded += OverlayWindow_Loaded;
    }

    private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Cover all screens
        var bounds = GetAllScreensBounds();
        this.Width = bounds.Width;
        this.Height = bounds.Height;
        this.Left = bounds.Left;
        this.Top = bounds.Top;
    }

    private Rect GetAllScreensBounds()
    {
        var rect = new Rect();
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            rect.Union(new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height));
        }
        return rect;
    }

    public void ShowCursorAt(Point position, string? responseText = null)
    {
        // Position cursor
        CursorEllipse.Margin = new Thickness(position.X - 10, position.Y - 10, 0, 0);

        // Show response if provided
        if (!string.IsNullOrEmpty(responseText))
        {
            ResponseTextBlock.Text = responseText;
            ResponseBorder.Visibility = Visibility.Visible;
            ResponseBorder.Margin = new Thickness(position.X + 30, position.Y + 10, 0, 0);
        }
        else
        {
            ResponseBorder.Visibility = Visibility.Collapsed;
        }

        // Animate cursor
        if (_cursorAnimation == null)
        {
            _cursorAnimation = new Storyboard();
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.2,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(scaleAnimation, CursorEllipse);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            _cursorAnimation.Children.Add(scaleAnimation);

            var scaleYAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.2,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(scaleYAnimation, CursorEllipse);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            _cursorAnimation.Children.Add(scaleYAnimation);

            CursorEllipse.RenderTransform = new ScaleTransform(1, 1);
        }
        _cursorAnimation.Begin();

        this.Show();
        this.Activate();
    }

    public void AnimateCursorTo(Point targetPosition)
    {
        // Simple animation to move cursor to target
        var animationX = new DoubleAnimation
        {
            To = targetPosition.X - 10,
            Duration = TimeSpan.FromSeconds(1.0),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        CursorEllipse.BeginAnimation(Canvas.LeftProperty, animationX);

        var animationY = new DoubleAnimation
        {
            To = targetPosition.Y - 10,
            Duration = TimeSpan.FromSeconds(1.0),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        CursorEllipse.BeginAnimation(Canvas.TopProperty, animationY);
    }

    public void FadeOut()
    {
        if (_fadeTimer == null)
        {
            _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _fadeTimer.Tick += (s, args) =>
            {
                this.Opacity -= 0.1;
                if (this.Opacity <= 0)
                {
                    _fadeTimer.Stop();
                    this.Hide();
                    this.Opacity = 1;
                }
            };
        }
        _fadeTimer.Start();
    }

    public void UpdateWaveform(float[] levels)
    {
        WaveformCanvas.Children.Clear();
        for (int i = 0; i < levels.Length && i < 50; i++)
        {
            var rect = new Rectangle
            {
                Width = 3,
                Height = levels[i] * 20,
                Fill = Brushes.White,
                Margin = new Thickness(i * 4, 20 - levels[i] * 20, 0, 0)
            };
            WaveformCanvas.Children.Add(rect);
        }
        WaveformCanvas.Visibility = Visibility.Visible;
    }
}