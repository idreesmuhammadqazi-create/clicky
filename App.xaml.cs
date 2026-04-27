using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace ClickyWindows;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up tray icon
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application; // Placeholder icon
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Clicky";

        // Add context menu or handle clicks
        _notifyIcon.DoubleClick += (s, args) => ShowPanel();

        // Set shutdown mode to explicit
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private void ShowPanel()
    {
        // TODO: Implement floating panel similar to macOS companion panel
        MessageBox.Show("Panel not implemented yet");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}