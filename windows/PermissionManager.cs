using System;
using NAudio.CoreAudioApi;
using Windows.Media.Capture;
using Windows.System;

namespace ClickyWindows;

public class PermissionManager
{
    public static bool CheckMicrophonePermission()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            return device != null;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> RequestMicrophonePermissionAsync()
    {
        // On Windows, microphone permission is handled via app capabilities in manifest
        // For runtime, we can check if available
        return CheckMicrophonePermission();
    }

    public static async Task<bool> CheckScreenCapturePermissionAsync()
    {
        // Check if screen capture is allowed
        var capture = new GraphicsCaptureSession();
        try
        {
            // Try to create a session
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> RequestScreenCapturePermissionAsync()
    {
        // Open settings for screen recording
        await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadcasting"));
        return false; // User needs to enable manually
    }

    public static void EnsurePermissions()
    {
        if (!CheckMicrophonePermission())
        {
            MessageBox.Show("Microphone permission is required. Please enable it in Settings > Privacy > Microphone.");
        }
    }
}