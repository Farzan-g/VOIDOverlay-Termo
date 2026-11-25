using System.Windows;
using System.Windows.Media;
using VOIDOverlay.Services;

namespace VOIDOverlay.Tests;

public sealed class SettingsThemeTests
{
    [Fact]
    public void LightAndDarkThemesReplaceTheCompletePalette()
    {
        var resources = new ResourceDictionary();

        SettingsThemeManager.Apply(resources, "Light");
        var lightWindow = Assert.IsType<SolidColorBrush>(resources["SettingsWindowBrush"]);
        var lightPopup = Assert.IsType<SolidColorBrush>(resources["SettingsPopupBrush"]);
        Assert.Equal(Color.FromRgb(243, 245, 248), lightWindow.Color);
        Assert.Equal(Color.FromRgb(255, 255, 255), lightPopup.Color);

        SettingsThemeManager.Apply(resources, "Dark");
        var darkWindow = Assert.IsType<SolidColorBrush>(resources["SettingsWindowBrush"]);
        var darkPopup = Assert.IsType<SolidColorBrush>(resources["SettingsPopupBrush"]);
        Assert.Equal(Color.FromRgb(23, 28, 34), darkWindow.Color);
        Assert.Equal(Color.FromRgb(41, 52, 64), darkPopup.Color);
        Assert.Single(resources.MergedDictionaries);
    }
}
