using Microsoft.Win32;

namespace ClickyWindows;

public static class StartupManager
{
    private const string AppName = "Clicky";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void RegisterForStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        key?.SetValue(AppName, System.Reflection.Assembly.GetExecutingAssembly().Location);
    }

    public static void UnregisterFromStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        key?.DeleteValue(AppName, false);
    }
}