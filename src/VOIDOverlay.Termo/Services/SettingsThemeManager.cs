using System.Diagnostics;
using System.IO;
using System.Security;
using System.Windows;
using Microsoft.Win32;

namespace VOIDOverlay.Services;

public static class SettingsThemeManager
{
    public static void Apply(ResourceDictionary applicationResources, string theme)
    {
        var useLightTheme = theme switch
        {
            "Light" => true,
            "Dark" => false,
            _ => IsWindowsLightTheme()
        };
        var palette = useLightTheme ? "Light" : "Dark";

        for (var index = applicationResources.MergedDictionaries.Count - 1; index >= 0; index--)
        {
            var source = applicationResources.MergedDictionaries[index].Source?.OriginalString;
            if (source?.Contains("SettingsLight.xaml", StringComparison.OrdinalIgnoreCase) == true ||
                source?.Contains("SettingsDark.xaml", StringComparison.OrdinalIgnoreCase) == true)
            {
                applicationResources.MergedDictionaries.RemoveAt(index);
            }
        }

        applicationResources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/VOIDOverlay.Termo;component/Themes/Settings{palette}.xaml",
                UriKind.Absolute)
        });
    }

    private static bool IsWindowsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value != 0;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            Trace.WriteLine($"Unable to read the Windows app theme; using the dark palette. {exception.Message}");
            return false;
        }
    }
}
