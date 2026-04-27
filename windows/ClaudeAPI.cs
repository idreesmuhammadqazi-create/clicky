using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClickyWindows;

public class ClaudeAPI
{
    private static readonly object TlsWarmupLock = new object();
    private static bool HasStartedTlsWarmup = false;

    private readonly Uri _apiUrl;
    private readonly HttpClient _httpClient;

    public string Model { get; set; }

    public ClaudeAPI(string proxyUrl, string model = "claude-sonnet-4-6")
    {
        _apiUrl = new Uri(proxyUrl);
        Model = model;

        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(300)
        };

        WarmUpTlsConnectionIfNeeded();
    }

    private void WarmUpTlsConnectionIfNeeded()
    {
        lock (TlsWarmupLock)
        {
            if (HasStartedTlsWarmup) return;
            HasStartedTlsWarmup = true;
        }

        // Fire a lightweight HEAD request to establish TLS
        Task.Run(async () =>
        {
            try
            {
                var warmupUrl = new Uri(_apiUrl, "/");
                await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, warmupUrl));
            }
            catch
            {
                // Ignore errors
            }
        });
    }

    private static string DetectImageMediaType(byte[] imageData)
    {
        if (imageData.Length >= 4)
        {
            // PNG signature: 89 50 4E 47
            if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
            {
                return "image/png";
            }
        }
        return "image/jpeg";
    }

    public async Task<(string text, double duration)> AnalyzeImageStreamingAsync(
        List<(byte[] data, string label)> images,
        string systemPrompt,
        List<(string userPlaceholder, string assistantResponse)> conversationHistory,
        string userPrompt,
        Action<string> onTextChunk)
    {
        var startTime = DateTime.Now;

        var messages = new List<object>();

        foreach (var (userPlaceholder, assistantResponse) in conversationHistory)
        {
            messages.Add(new { role = "user", content = userPlaceholder });
            messages.Add(new { role = "assistant", content = assistantResponse });
        }

        var contentBlocks = new List<object>();
        foreach (var image in images)
        {
            contentBlocks.Add(new
            {
                type = "image",
                source = new
                {
                    type = "base64",
                    media_type = DetectImageMediaType(image.data),
                    data = Convert.ToBase64String(image.data)
                }
            });
            contentBlocks.Add(new { type = "text", text = image.label });
        }
        contentBlocks.Add(new { type = "text", text = userPrompt });

        messages.Add(new { role = "user", content = contentBlocks });

        var body = new
        {
            model = Model,
            max_tokens = 1024,
            stream = true,
            system = systemPrompt,
            messages
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var accumulatedResponseText = "";

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            if (!line.StartsWith("data: ")) continue;
            var jsonString = line.Substring(6);

            if (jsonString == "[DONE]") break;

            try
            {
                var eventPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                if (eventPayload == null) continue;

                if (eventPayload.TryGetValue("type", out var eventTypeObj) && eventTypeObj?.ToString() == "content_block_delta")
                {
                    if (eventPayload.TryGetValue("delta", out var deltaObj))
                    {
                        var delta = deltaObj as Dictionary<string, object>;
                        if (delta != null &&
                            delta.TryGetValue("type", out var deltaTypeObj) && deltaTypeObj?.ToString() == "text_delta" &&
                            delta.TryGetValue("text", out var textChunkObj))
                        {
                            var textChunk = textChunkObj?.ToString() ?? "";
                            accumulatedResponseText += textChunk;
                            onTextChunk(accumulatedResponseText);
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        var duration = (DateTime.Now - startTime).TotalSeconds;
        return (accumulatedResponseText, duration);
    }
}