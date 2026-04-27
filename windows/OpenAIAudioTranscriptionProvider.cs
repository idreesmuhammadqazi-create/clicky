using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClickyWindows;

public interface ITranscriptionProvider
{
    string DisplayName { get; }
    bool IsConfigured { get; }
    string? UnavailableExplanation { get; }
    Task<ITranscriptionSession> StartStreamingSessionAsync(
        List<string> keyterms,
        Action<string> onTranscriptUpdate,
        Action<string> onFinalTranscriptReady,
        Action<Exception> onError);
}

public interface ITranscriptionSession : IDisposable
{
    double FinalTranscriptFallbackDelaySeconds { get; }
    void AppendAudioBuffer(byte[] audioData);
    void RequestFinalTranscript();
    void Cancel();
}

public class OpenAIAudioTranscriptionProvider : ITranscriptionProvider
{
    private readonly string? _apiKey;
    private readonly string _modelName;

    public OpenAIAudioTranscriptionProvider(string? apiKey = null, string modelName = "whisper-1")
    {
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _modelName = modelName;
    }

    public string DisplayName => "OpenAI";

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public string? UnavailableExplanation => !IsConfigured ? "OpenAI transcription is not configured. Set OPENAI_API_KEY environment variable." : null;

    public async Task<ITranscriptionSession> StartStreamingSessionAsync(
        List<string> keyterms,
        Action<string> onTranscriptUpdate,
        Action<string> onFinalTranscriptReady,
        Action<Exception> onError)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(UnavailableExplanation);
        }

        return new OpenAIAudioTranscriptionSession(_apiKey!, _modelName, keyterms, onTranscriptUpdate, onFinalTranscriptReady, onError);
    }
}

internal class OpenAIAudioTranscriptionSession : ITranscriptionSession
{
    private class TranscriptionResponse
    {
        public string text { get; set; } = "";
    }

    private const string TranscriptionUrl = "https://api.openai.com/v1/audio/transcriptions";
    private const int TargetSampleRate = 16000;

    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly List<string> _keyterms;
    private readonly Action<string> _onTranscriptUpdate;
    private readonly Action<string> _onFinalTranscriptReady;
    private readonly Action<Exception> _onError;

    private readonly HttpClient _httpClient;
    private readonly object _stateLock = new object();

    private byte[] _bufferedAudioData = Array.Empty<byte>();
    private bool _hasRequestedFinalTranscript;
    private bool _hasDeliveredFinalTranscript;
    private bool _isCancelled;
    private Task? _transcriptionTask;

    public double FinalTranscriptFallbackDelaySeconds => 8.0;

    public OpenAIAudioTranscriptionSession(
        string apiKey,
        string modelName,
        List<string> keyterms,
        Action<string> onTranscriptUpdate,
        Action<string> onFinalTranscriptReady,
        Action<Exception> onError)
    {
        _apiKey = apiKey;
        _modelName = modelName;
        _keyterms = keyterms;
        _onTranscriptUpdate = onTranscriptUpdate;
        _onFinalTranscriptReady = onFinalTranscriptReady;
        _onError = onError;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
    }

    public void AppendAudioBuffer(byte[] audioData)
    {
        lock (_stateLock)
        {
            if (_hasRequestedFinalTranscript || _isCancelled) return;
            var newBuffer = new byte[_bufferedAudioData.Length + audioData.Length];
            _bufferedAudioData.CopyTo(newBuffer, 0);
            audioData.CopyTo(newBuffer, _bufferedAudioData.Length);
            _bufferedAudioData = newBuffer;
        }
    }

    public void RequestFinalTranscript()
    {
        lock (_stateLock)
        {
            if (_hasRequestedFinalTranscript || _isCancelled) return;
            _hasRequestedFinalTranscript = true;

            var audioData = _bufferedAudioData;
            _transcriptionTask = Task.Run(() => TranscribeBufferedAudioAsync(audioData));
        }
    }

    public void Cancel()
    {
        lock (_stateLock)
        {
            _isCancelled = true;
            _bufferedAudioData = Array.Empty<byte>();
        }
        _transcriptionTask?.Wait();
        _httpClient.Dispose();
    }

    public void Dispose()
    {
        Cancel();
    }

    private async Task TranscribeBufferedAudioAsync(byte[] bufferedAudioData)
    {
        bool isCancelled;
        lock (_stateLock)
        {
            isCancelled = _isCancelled;
        }
        if (isCancelled || bufferedAudioData.Length == 0)
        {
            DeliverFinalTranscript("");
            return;
        }

        // Convert to WAV if needed (assuming PCM16)
        var wavAudioData = BuildWavData(bufferedAudioData, TargetSampleRate);

        try
        {
            var transcriptText = await RequestTranscriptionAsync(wavAudioData);
            lock (_stateLock)
            {
                isCancelled = _isCancelled;
            }
            if (isCancelled) return;

            if (!string.IsNullOrEmpty(transcriptText))
            {
                _onTranscriptUpdate(transcriptText);
            }

            DeliverFinalTranscript(transcriptText);
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                isCancelled = _isCancelled;
            }
            if (isCancelled) return;
            _onError(ex);
        }
    }

    private async Task<string> RequestTranscriptionAsync(byte[] wavAudioData)
    {
        var boundary = $"Boundary-{Guid.NewGuid()}";
        var request = new HttpRequestMessage(HttpMethod.Post, TranscriptionUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new MultipartFormDataContent(boundary);

        ((MultipartFormDataContent)request.Content).Add(new StringContent(_modelName), "model");
        ((MultipartFormDataContent)request.Content).Add(new StringContent("en"), "language");
        ((MultipartFormDataContent)request.Content).Add(new StringContent("json"), "response_format");

        var prompt = TranscriptionPromptText();
        if (!string.IsNullOrEmpty(prompt))
        {
            ((MultipartFormDataContent)request.Content).Add(new StringContent(prompt), "prompt");
        }

        var fileContent = new ByteArrayContent(wavAudioData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        ((MultipartFormDataContent)request.Content).Add(fileContent, "file", "voice-input.wav");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        var transcriptionResponse = JsonSerializer.Deserialize<TranscriptionResponse>(responseData);
        return transcriptionResponse?.text?.Trim() ?? "";
    }

    private string? TranscriptionPromptText()
    {
        var normalizedKeyterms = _keyterms
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList();

        if (!normalizedKeyterms.Any()) return null;

        return $"This is a short push-to-talk transcript for a coding and product app. Expect product names, technical terms, and app-specific vocabulary such as: {string.Join(", ", normalizedKeyterms)}.";
    }

    private void DeliverFinalTranscript(string transcriptText)
    {
        if (_hasDeliveredFinalTranscript) return;
        _hasDeliveredFinalTranscript = true;
        _onFinalTranscriptReady(transcriptText);
    }

    private static byte[] BuildWavData(byte[] pcm16MonoAudio, int sampleRate)
    {
        // Simple WAV header for PCM16 mono
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // WAV header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcm16MonoAudio.Length); // File size
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // PCM format chunk size
        writer.Write((short)1); // PCM
        writer.Write((short)1); // Mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm16MonoAudio.Length);
        writer.Write(pcm16MonoAudio);

        return ms.ToArray();
    }
}