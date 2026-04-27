using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace ClickyWindows;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private CompanionPanel? _companionPanel;
    private GlobalHotkeyMonitor? _hotkeyMonitor;
    private AudioCaptureManager? _audioCapture;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up tray icon
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = SystemIcons.Application; // Placeholder icon - replace with actual Clicky icon
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Clicky";

        // Set up context menu
        var contextMenu = new ContextMenuStrip();
        var showPanelMenuItem = new ToolStripMenuItem("Show Panel");
        showPanelMenuItem.Click += (s, args) => ShowPanel();
        contextMenu.Items.Add(showPanelMenuItem);

        var quitMenuItem = new ToolStripMenuItem("Quit");
        quitMenuItem.Click += (s, args) => Shutdown();
        contextMenu.Items.Add(quitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Handle single click to show panel
        _notifyIcon.Click += (s, args) => ShowPanel();

        // Set up global hotkey monitor
        _hotkeyMonitor = new GlobalHotkeyMonitor();
        _hotkeyMonitor.HotkeyPressed += OnHotkeyPressed;
        _hotkeyMonitor.HotkeyReleased += OnHotkeyReleased;

        // Set up audio capture
        _audioCapture = new AudioCaptureManager();
        _audioCapture.AudioDataAvailable += OnAudioDataAvailable;

        // Set shutdown mode to explicit
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Start voice recording
        _audioCapture?.StartRecording();
        // TODO: Update UI status
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        // Stop voice recording and process
        _audioCapture?.StopRecording();
        // TODO: Send to transcription
    }

    private void OnAudioDataAvailable(object? sender, byte[] audioData)
    {
        // TODO: Process audio data in real-time if needed
    }

    private void ShowPanel()
    {
        if (_companionPanel == null)
        {
            _companionPanel = new CompanionPanel();
            _companionPanel.Closed += (s, args) => _companionPanel = null;
        }
        _companionPanel.Show();
        _companionPanel.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _audioCapture?.Dispose();
        _hotkeyMonitor?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}