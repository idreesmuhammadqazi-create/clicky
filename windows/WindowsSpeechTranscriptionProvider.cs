using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace ClickyWindows;

public class WindowsSpeechTranscriptionProvider : ITranscriptionProvider
{
    public string DisplayName => "Windows Speech";

    public bool IsConfigured => true;

    public string? UnavailableExplanation => null;

    public async Task<ITranscriptionSession> StartStreamingSessionAsync(
        List<string> keyterms,
        Action<string> onTranscriptUpdate,
        Action<string> onFinalTranscriptReady,
        Action<Exception> onError)
    {
        return new WindowsSpeechTranscriptionSession(onTranscriptUpdate, onFinalTranscriptReady, onError);
    }
}

internal class WindowsSpeechTranscriptionSession : ITranscriptionSession
{
    private readonly Action<string> _onTranscriptUpdate;
    private readonly Action<string> _onFinalTranscriptReady;
    private readonly Action<Exception> _onError;

    private SpeechRecognitionEngine? _recognizer;
    private bool _isListening;
    private string _accumulatedText = "";

    public double FinalTranscriptFallbackDelaySeconds => 2.0;

    public WindowsSpeechTranscriptionSession(
        Action<string> onTranscriptUpdate,
        Action<string> onFinalTranscriptReady,
        Action<Exception> onError)
    {
        _onTranscriptUpdate = onTranscriptUpdate;
        _onFinalTranscriptReady = onFinalTranscriptReady;
        _onError = onError;

        try
        {
            _recognizer = new SpeechRecognitionEngine();
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.LoadGrammar(new DictationGrammar());

            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.SpeechRecognitionRejected += OnSpeechRejected;
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
    }

    public void AppendAudioBuffer(byte[] audioData)
    {
        // Windows Speech uses its own audio input, so ignore
    }

    public void RequestFinalTranscript()
    {
        if (_recognizer != null && _isListening)
        {
            _recognizer.RecognizeAsyncStop();
            _isListening = false;
            _onFinalTranscriptReady(_accumulatedText);
        }
    }

    public void Cancel()
    {
        if (_recognizer != null)
        {
            _recognizer.RecognizeAsyncStop();
            _recognizer.Dispose();
            _recognizer = null;
        }
        _isListening = false;
    }

    public void Dispose()
    {
        Cancel();
    }

    public void StartListening()
    {
        if (_recognizer != null && !_isListening)
        {
            _isListening = true;
            _accumulatedText = "";
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _accumulatedText += e.Result.Text + " ";
        _onTranscriptUpdate(_accumulatedText.Trim());
    }

    private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
    {
        // Ignore or handle rejection
    }
}