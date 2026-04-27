# Clicky Windows Port

This is a complete Windows port of Clicky, the macOS menu bar AI companion app. It provides voice-controlled interactions with Claude using screenshots and ElevenLabs TTS.

## Features

- Tray icon with context menu
- Floating companion panel
- Push-to-talk with Ctrl+Alt hotkey
- Audio capture and transcription (OpenAI or Windows Speech)
- Multi-monitor screen capture
- Claude vision API integration with streaming responses
- ElevenLabs TTS playback
- Transparent overlay with cursor animations and element pointing
- Permission checking
- Analytics with PostHog
- Startup registration

## Prerequisites

- .NET 8 SDK
- Windows 10/11
- Microphone and screen capture permissions
- API keys for Claude (via Cloudflare Worker), ElevenLabs, and optionally OpenAI

## Setup

1. Clone or download the project files to a directory.

2. Set environment variables for API keys:
   ```
   set OPENAI_API_KEY=your_openai_key
   set CLAUDE_PROXY_URL=https://your-worker.workers.dev/chat
   set ELEVENLABS_PROXY_URL=https://your-worker.workers.dev/tts
   set POSTHOG_API_KEY=your_posthog_key
   ```

3. Install NuGet packages:
   ```
   dotnet restore
   ```

## Build

```
dotnet build --configuration Release
```

## Run

```
dotnet run --project windows/ClickyWindows.csproj
```

The app will appear in the system tray. Right-click the tray icon to show the panel or quit.

## Testing

1. Click the tray icon to show the companion panel.
2. Press Ctrl+Alt to start recording voice input.
3. Speak a command, e.g., "What is this window?"
4. Release Ctrl+Alt to process.
5. The app will capture screenshots, transcribe audio, send to Claude, stream response, play TTS, and animate cursor if pointing.

## Packaging

To create an MSI installer:

1. Install WiX Toolset.

2. Use `dotnet publish` to create a self-contained app.

3. Use WiX to create MSI from the published files.

For Microsoft Store, package as MSIX.

## Notes

- Replace placeholder proxy URLs with your actual Cloudflare Worker URLs.
- The app requires microphone and screen recording permissions.
- For production, add proper error handling and logging.
- The overlay covers all monitors; ensure no conflicts with other apps.

## Troubleshooting

- If hotkey doesn't work, check if other apps are using Ctrl+Alt.
- If audio doesn't capture, verify microphone permissions.
- If Claude fails, check proxy and API keys.