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
    private CompanionManager? _companionManager;
    private OverlayWindow? _overlay;
    private ClaudeAPI? _claudeApi;
    private ElevenLabsTTSClient? _ttsClient;
    private ITranscriptionProvider? _transcriptionProvider;

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

        // Check permissions
        PermissionManager.EnsurePermissions();

        // Register for startup
        StartupManager.RegisterForStartup();

        // Set up components
        _hotkeyMonitor = new GlobalHotkeyMonitor();
        _audioCapture = new AudioCaptureManager();
        _overlay = new OverlayWindow();
        _claudeApi = new ClaudeAPI("https://your-worker-name.your-subdomain.workers.dev/chat", "claude-sonnet-4-6");
        _ttsClient = new ElevenLabsTTSClient("https://your-worker-name.your-subdomain.workers.dev/tts");

        // Choose transcription provider
        _transcriptionProvider = new OpenAIAudioTranscriptionProvider(); // or WindowsSpeechTranscriptionProvider

        _companionPanel = new CompanionPanel();

        // Set up companion manager
        _companionManager = new CompanionManager(
            _hotkeyMonitor,
            _audioCapture,
            _transcriptionProvider,
            _claudeApi,
            _ttsClient,
            _overlay,
            _companionPanel);

        // Set shutdown mode to explicit
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private void ShowPanel()
    {
        if (_companionPanel == null)
        {
            _companionPanel = new CompanionPanel();
        }
        _companionPanel.Show();
        _companionPanel.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _companionManager = null;
        _overlay?.Close();
        _audioCapture?.Dispose();
        _hotkeyMonitor?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}