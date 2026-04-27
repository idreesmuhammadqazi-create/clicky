using System.Windows;
using System.Windows.Media;

namespace ClickyWindows;

public static class DS
{
    public static class Colors
    {
        public static readonly Color Primary = Color.FromRgb(0, 123, 255);
        public static readonly Color Background = Color.FromRgb(26, 26, 26);
        public static readonly Color Surface = Color.FromRgb(33, 33, 33);
        public static readonly Color Text = Colors.White;
        public static readonly Color TextSecondary = Color.FromRgb(204, 204, 204);
        public static readonly Color Border = Color.FromRgb(51, 51, 51);
    }

    public static class CornerRadius
    {
        public static readonly CornerRadius Small = new CornerRadius(4);
        public static readonly CornerRadius Medium = new CornerRadius(8);
        public static readonly CornerRadius Large = new CornerRadius(10);
    }

    public static class Styles
    {
        public static readonly Style ButtonStyle = new Style(typeof(System.Windows.Controls.Button));
        // Initialize styles...
    }
}