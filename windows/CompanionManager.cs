using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ClickyWindows;

public enum VoiceState
{
    Idle,
    Listening,
    Processing,
    Responding
}

public class CompanionManager
{
    private VoiceState _voiceState = VoiceState.Idle;
    private readonly GlobalHotkeyMonitor _hotkeyMonitor;
    private readonly AudioCaptureManager _audioCapture;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ITranscriptionSession? _transcriptionSession;
    private readonly ClaudeAPI _claudeApi;
    private readonly ElevenLabsTTSClient _ttsClient;
    private readonly OverlayWindow _overlay;
    private readonly CompanionPanel _panel;

    private List<(byte[] data, string label)> _currentScreenshots = new();
    private string _currentTranscript = "";
    private List<(string user, string assistant)> _conversationHistory = new();

    private DispatcherTimer? _fallbackTimer;

    public CompanionManager(
        GlobalHotkeyMonitor hotkeyMonitor,
        AudioCaptureManager audioCapture,
        ITranscriptionProvider transcriptionProvider,
        ClaudeAPI claudeApi,
        ElevenLabsTTSClient ttsClient,
        OverlayWindow overlay,
        CompanionPanel panel)
    {
        _hotkeyMonitor = hotkeyMonitor;
        _audioCapture = audioCapture;
        _transcriptionProvider = transcriptionProvider;
        _claudeApi = claudeApi;
        _ttsClient = ttsClient;
        _overlay = overlay;
        _panel = panel;

        _hotkeyMonitor.HotkeyPressed += OnHotkeyPressed;
        _hotkeyMonitor.HotkeyReleased += OnHotkeyReleased;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (_voiceState != VoiceState.Idle) return;

        _voiceState = VoiceState.Listening;
        _audioCapture.StartRecording();

        // Capture screenshots
        _currentScreenshots = ScreenCaptureUtility.CaptureAllScreens()
            .Select(s => (s.imageData, s.screenName))
            .ToList();

        // Show overlay
        var cursorPos = System.Windows.Forms.Control.MousePosition;
        _overlay.ShowCursorAt(new Point(cursorPos.X, cursorPos.Y));

        // Start transcription session
        try
        {
            _transcriptionSession = _transcriptionProvider.StartStreamingSessionAsync(
                new List<string> { "coding", "app", "clicky" },
                OnTranscriptUpdate,
                OnFinalTranscriptReady,
                OnTranscriptionError).Result;
        }
        catch
        {
            // Fallback
        }
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        if (_voiceState != VoiceState.Listening) return;

        _voiceState = VoiceState.Processing;
        _audioCapture.StopRecording();

        // Stop transcription
        _transcriptionSession?.RequestFinalTranscript();

        // Start fallback timer
        _fallbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_transcriptionSession?.FinalTranscriptFallbackDelaySeconds ?? 2.0) };
        _fallbackTimer.Tick += (s, args) =>
        {
            _fallbackTimer.Stop();
            if (_voiceState == VoiceState.Processing)
            {
                OnFinalTranscriptReady(_currentTranscript);
            }
        };
        _fallbackTimer.Start();
    }

    private void OnTranscriptUpdate(string transcript)
    {
        _currentTranscript = transcript;
        // Update overlay text
        _overlay.ShowCursorAt(_overlay.CursorEllipse.TransformToAncestor(_overlay).Transform(new Point(10, 10)), transcript);
    }

    private void OnFinalTranscriptReady(string transcript)
    {
        if (_voiceState != VoiceState.Processing) return;

        _voiceState = VoiceState.Responding;
        _currentTranscript = transcript;

        // Send to Claude
        Task.Run(async () =>
        {
            try
            {
                var systemPrompt = "You are Clicky, a helpful AI assistant for coding and productivity. Be concise and actionable.";
                var result = await _claudeApi.AnalyzeImageStreamingAsync(
                    _currentScreenshots,
                    systemPrompt,
                    _conversationHistory.Select(h => (h.user, h.assistant)).ToList(),
                    transcript,
                    OnClaudeTextChunk);

                // Parse pointing tags
                ParsePointingTags(result.text);

                // Speak response
                await _ttsClient.SpeakTextAsync(result.text);

                // Add to history
                _conversationHistory.Add((transcript, result.text));

                // Fade out overlay
                Application.Current.Dispatcher.Invoke(() => _overlay.FadeOut());
                _voiceState = VoiceState.Idle;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        });
    }

    private void OnClaudeTextChunk(string text)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var cursorPos = new Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y);
            _overlay.ShowCursorAt(cursorPos, text);
        });
    }

    private void ParsePointingTags(string response)
    {
        // Parse [POINT:x,y:label:screenN] tags
        var regex = new System.Text.RegularExpressions.Regex(@"\[POINT:(\d+),(\d+):([^:]+):(\d+)\]");
        var matches = regex.Matches(response);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var x = int.Parse(match.Groups[1].Value);
            var y = int.Parse(match.Groups[2].Value);
            var label = match.Groups[3].Value;
            var screenN = int.Parse(match.Groups[4].Value);

            // Map to screen coordinates (simplified)
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screenN < screens.Length)
            {
                var screen = screens[screenN];
                var point = new Point(screen.Bounds.X + x, screen.Bounds.Y + y);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlay.AnimateCursorTo(new System.Windows.Point(point.X, point.Y));
                });
            }
        }
    }

    private void OnTranscriptionError(Exception ex)
    {
        // Handle error
        _voiceState = VoiceState.Idle;
    }

    private void OnError(Exception ex)
    {
        _voiceState = VoiceState.Idle;
        // Show error in panel or overlay
    }
}