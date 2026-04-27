using PostHog;
using System;

namespace ClickyWindows;

public static class ClickyAnalytics
{
    private static PostHogClient? _client;

    static ClickyAnalytics()
    {
        _client = new PostHogClient("your-posthog-api-key", "https://app.posthog.com");
    }

    public static void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        _client?.Capture("user_id", eventName, properties);
    }

    public static void Identify(string userId, Dictionary<string, object>? properties = null)
    {
        _client?.Identify(userId, properties);
    }
}