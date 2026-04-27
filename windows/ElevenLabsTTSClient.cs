using NAudio.Wave;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ClickyWindows;

public class ElevenLabsTTSClient
{
    private readonly Uri _proxyUrl;
    private readonly HttpClient _httpClient;
    private WaveOutEvent? _waveOut;
    private Mp3FileReader? _mp3Reader;

    public ElevenLabsTTSClient(string proxyUrl)
    {
        _proxyUrl = new Uri(proxyUrl);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task SpeakTextAsync(string text)
    {
        var body = new
        {
            text,
            model_id = "eleven_flash_v2_5",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _proxyUrl)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("audio/mpeg"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var audioData = await response.Content.ReadAsByteArrayAsync();

        // Stop any existing playback
        StopPlayback();

        // Play the audio
        var stream = new MemoryStream(audioData);
        _mp3Reader = new Mp3FileReader(stream);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_mp3Reader);
        _waveOut.Play();
    }

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    public void StopPlayback()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _mp3Reader?.Dispose();
        _mp3Reader = null;
    }
}