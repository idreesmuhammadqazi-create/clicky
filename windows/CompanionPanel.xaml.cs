using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace ClickyWindows;

public partial class CompanionPanel : Window
{
    public CompanionPanel()
    {
        InitializeComponent();
        this.Loaded += CompanionPanel_Loaded;
        this.Deactivated += CompanionPanel_Deactivated;
    }

    private void CompanionPanel_Loaded(object sender, RoutedEventArgs e)
    {
        // Position near mouse cursor or center screen
        var mousePos = System.Windows.Forms.Control.MousePosition;
        this.Left = mousePos.X - this.Width / 2;
        this.Top = mousePos.Y - this.Height / 2;

        // Ensure within screen bounds
        var screen = Screen.FromPoint(mousePos);
        if (this.Left < screen.WorkingArea.Left) this.Left = screen.WorkingArea.Left;
        if (this.Top < screen.WorkingArea.Top) this.Top = screen.WorkingArea.Top;
        if (this.Left + this.Width > screen.WorkingArea.Right) this.Left = screen.WorkingArea.Right - this.Width;
        if (this.Top + this.Height > screen.WorkingArea.Bottom) this.Top = screen.WorkingArea.Bottom - this.Height;
    }

    private void CompanionPanel_Deactivated(object sender, EventArgs e)
    {
        // Auto-hide when focus lost (equivalent to outside click dismiss)
        this.Hide();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}