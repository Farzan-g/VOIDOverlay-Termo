using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using VOIDOverlay.Models;
using VOIDOverlay.Services;
using Forms = System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace VOIDOverlay;

public partial class OverlaySettingsWindow : Window
{
    private readonly OverlaySettings _settings;
    private readonly App _app;
    private readonly DispatcherTimer _saveTimer;
    private bool _initialized;
    private bool _synchronizingOffsets;
    private bool _updatingPresetSelection;
    private bool _allowClose;

    public OverlaySettingsWindow(OverlaySettings settings, App app)
    {
        InitializeComponent();
        _settings = settings;
        _app = app;
        Owner = System.Windows.Application.Current.MainWindow;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            _app.SaveSettings();
        };
        MonitorCombo.ItemsSource = GetMonitorOptions();
        LoadSettings();
        WireChanges();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (_, _) => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        Closing += OnClosing;
    }

    public void CloseForExit()
    {
        _allowClose = true;
        _saveTimer.Stop();
        Close();
    }

    private bool PositionControlsEnabled
    {
        set
        {
            PositionCombo.IsEnabled = value;
            MonitorCombo.IsEnabled = value;
            EdgeReferenceCombo.IsEnabled = value;
            OffsetXSlider.IsEnabled = value;
            OffsetYSlider.IsEnabled = value;
            OffsetXBox.IsEnabled = value;
            OffsetYBox.IsEnabled = value;
        }
    }

    private void LoadSettings()
    {
        _initialized = false;
        StartupCheck.IsChecked = _settings.StartWithWindows;
        TopmostCheck.IsChecked = _settings.AlwaysOnTop;
        LockedCheck.IsChecked = _settings.IsLocked;
        ClickThroughCheck.IsChecked = _settings.ClickThrough;
        StartVisibleCheck.IsChecked = _settings.IsOverlayVisible;
        BackgroundEnabledCheck.IsChecked = _settings.BackgroundEnabled;
        BackgroundColorBox.Text = _settings.BackgroundColor;
        BackgroundOpacitySlider.Value = _settings.BackgroundOpacity;
        OverlayOpacitySlider.Value = _settings.Opacity;
        TextColorBox.Text = _settings.TextColor;
        AccentColorBox.Text = _settings.AccentColor;
        SecondaryAccentColorBox.Text = _settings.SecondaryAccentColor;
        FontFamilyCombo.SelectedValue = _settings.FontFamily;
        FontSizeSlider.Value = _settings.FontSize;
        ReadabilityCombo.SelectedValue = _settings.TextReadability;
        ThemeCombo.SelectedValue = _settings.Theme;
        IntervalCombo.SelectedValue = _settings.RefreshIntervalMs.ToString(CultureInfo.InvariantCulture);
        CpuTemperatureCheck.IsChecked = _settings.ShowCpuTemperature;
        GpuTemperatureCheck.IsChecked = _settings.ShowGpuTemperature;
        CpuGraphCheck.IsChecked = _settings.ShowCpuGraph;
        GpuGraphCheck.IsChecked = _settings.ShowGpuGraph;
        MetricSpacingSlider.Value = _settings.MetricSpacing;
        OverlayPaddingSlider.Value = _settings.OverlayPadding;
        PositionCombo.SelectedValue = _settings.Position.ToString();
        MonitorCombo.SelectedItem = MonitorCombo.Items
            .Cast<MonitorOption>()
            .FirstOrDefault(item => item.DeviceName.Equals(
                _settings.MonitorDeviceName,
                StringComparison.OrdinalIgnoreCase))
            ?? MonitorCombo.Items.Cast<MonitorOption>().FirstOrDefault(item =>
                item.DeviceName.Equals(
                    (Forms.Screen.PrimaryScreen?.DeviceName ?? string.Empty),
                    StringComparison.OrdinalIgnoreCase));
        EdgeReferenceCombo.SelectedIndex =
            _settings.EdgeReference == OverlayEdgeReference.WorkArea ? 0 : 1;
        OffsetXSlider.Value = _settings.HorizontalOffsetX;
        OffsetYSlider.Value = _settings.VerticalOffsetY;
        OffsetXBox.Text = FormatCoordinate(_settings.HorizontalOffsetX);
        OffsetYBox.Text = FormatCoordinate(_settings.VerticalOffsetY);
        UpdateOffsetRange();
        PositionControlsEnabled = !_settings.IsLocked;
        _initialized = true;
    }

    private void WireChanges()
    {
        StartupCheck.Checked += StartupChanged;
        StartupCheck.Unchecked += StartupChanged;
        TopmostCheck.Checked += SettingsChanged;
        TopmostCheck.Unchecked += SettingsChanged;
        LockedCheck.Checked += SettingsChanged;
        LockedCheck.Unchecked += SettingsChanged;
        ClickThroughCheck.Checked += SettingsChanged;
        ClickThroughCheck.Unchecked += SettingsChanged;
        StartVisibleCheck.Checked += SettingsChanged;
        StartVisibleCheck.Unchecked += SettingsChanged;
        BackgroundEnabledCheck.Checked += SettingsChanged;
        BackgroundEnabledCheck.Unchecked += SettingsChanged;
        BackgroundColorBox.LostFocus += SettingsChanged;
        BackgroundOpacitySlider.ValueChanged += SettingsChanged;
        OverlayOpacitySlider.ValueChanged += SettingsChanged;
        TextColorBox.LostFocus += SettingsChanged;
        AccentColorBox.LostFocus += SettingsChanged;
        SecondaryAccentColorBox.LostFocus += SettingsChanged;
        FontFamilyCombo.SelectionChanged += SettingsChanged;
        FontSizeSlider.ValueChanged += SettingsChanged;
        ReadabilityCombo.SelectionChanged += SettingsChanged;
        ThemeCombo.SelectionChanged += SettingsChanged;
        IntervalCombo.SelectionChanged += SettingsChanged;
        CpuTemperatureCheck.Checked += SettingsChanged;
        CpuTemperatureCheck.Unchecked += SettingsChanged;
        GpuTemperatureCheck.Checked += SettingsChanged;
        GpuTemperatureCheck.Unchecked += SettingsChanged;
        CpuGraphCheck.Checked += SettingsChanged;
        CpuGraphCheck.Unchecked += SettingsChanged;
        GpuGraphCheck.Checked += SettingsChanged;
        GpuGraphCheck.Unchecked += SettingsChanged;
        MetricSpacingSlider.ValueChanged += SettingsChanged;
        OverlayPaddingSlider.ValueChanged += SettingsChanged;
        OffsetXSlider.ValueChanged += OffsetSliderChanged;
        OffsetYSlider.ValueChanged += OffsetSliderChanged;
    }

    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        try
        {
            var enabled = StartupCheck.IsChecked == true;
            if (enabled != _settings.StartWithWindows)
            {
                _app.SetStartWithWindows(enabled);
            }

            ApplySettings();
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
        {
            System.Diagnostics.Trace.WriteLine($"Unable to update Termo startup registration: {exception}");
            StartupCheck.IsChecked = _settings.StartWithWindows;
        }
    }

    private void SettingsChanged(object sender, RoutedEventArgs e) => ApplySettings();

    private void PositionPresetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _updatingPresetSelection ||
            !Enum.TryParse<OverlayPosition>(PositionCombo.SelectedValue?.ToString(), out var preset) ||
            !TryGetSelectedDisplay(out var screen))
        {
            return;
        }

        var (monitorBounds, usableBounds, dpiScaleX, dpiScaleY) = GetSelectedDisplayBounds(screen);
        var (width, height) = _app.GetOverlaySize();
        var offsets = OverlayPositionCalculator.GetPresetOffsets(
            monitorBounds,
            usableBounds,
            dpiScaleX,
            dpiScaleY,
            width,
            height,
            preset);
        _settings.Position = preset;
        SetOffsetValues(offsets.X, offsets.Y);
        ApplySettings();
    }

    private void MonitorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        UpdateOffsetRange();
        ApplySettings();
    }

    private void BoundsChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialized)
        {
            UpdateOffsetRange();
            ApplySettings();
        }
    }

    private void OffsetSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || _synchronizingOffsets)
        {
            return;
        }

        var x = ReferenceEquals(sender, OffsetXSlider) ? OffsetXSlider.Value : _settings.HorizontalOffsetX;
        var y = ReferenceEquals(sender, OffsetYSlider) ? OffsetYSlider.Value : _settings.VerticalOffsetY;
        SetOffsetValues(x, y);
        ApplySettings();
    }

    private void OffsetTextChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized || _synchronizingOffsets)
        {
            return;
        }

        var box = (TextBox)sender;
        var isX = ReferenceEquals(box, OffsetXBox);
        if (!TryParseCoordinate(
                box.Text,
                isX ? OffsetXSlider.Minimum : OffsetYSlider.Minimum,
                isX ? OffsetXSlider.Maximum : OffsetYSlider.Maximum,
                out var offset))
        {
            box.ToolTip = "Enter a number within the current display range (up to two decimal places).";
            return;
        }

        box.ToolTip = null;
        SetOffsetValues(
            isX ? offset : _settings.HorizontalOffsetX,
            isX ? _settings.VerticalOffsetY : offset);
        ApplySettings();
    }

    private void OffsetKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OffsetTextChanged(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SetOffsetValues(double x, double y)
    {
        _synchronizingOffsets = true;
        try
        {
            x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
            y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
            OffsetXSlider.Value = Math.Clamp(x, OffsetXSlider.Minimum, OffsetXSlider.Maximum);
            OffsetYSlider.Value = Math.Clamp(y, OffsetYSlider.Minimum, OffsetYSlider.Maximum);
            OffsetXBox.Text = FormatCoordinate(OffsetXSlider.Value);
            OffsetYBox.Text = FormatCoordinate(OffsetYSlider.Value);
        }
        finally
        {
            _synchronizingOffsets = false;
        }
    }

    private void UpdateOffsetRange()
    {
        if (!TryGetSelectedDisplay(out var screen))
        {
            return;
        }

        var (monitorBounds, usableBounds, dpiScaleX, dpiScaleY) = GetSelectedDisplayBounds(screen);
        var (width, height) = _app.GetOverlaySize();
        var range = OverlayPositionCalculator.GetOffsetRange(
            monitorBounds,
            usableBounds,
            dpiScaleX,
            dpiScaleY,
            width,
            height);
        _synchronizingOffsets = true;
        try
        {
            OffsetXSlider.Minimum = range.MinimumX;
            OffsetXSlider.Maximum = range.MaximumX;
            OffsetYSlider.Minimum = range.MinimumY;
            OffsetYSlider.Maximum = range.MaximumY;
            OffsetXSlider.Value = Math.Clamp(_settings.HorizontalOffsetX, range.MinimumX, range.MaximumX);
            OffsetYSlider.Value = Math.Clamp(_settings.VerticalOffsetY, range.MinimumY, range.MaximumY);
            _settings.HorizontalOffsetX = Math.Round(OffsetXSlider.Value, 2, MidpointRounding.AwayFromZero);
            _settings.VerticalOffsetY = Math.Round(OffsetYSlider.Value, 2, MidpointRounding.AwayFromZero);
            OffsetXBox.Text = FormatCoordinate(_settings.HorizontalOffsetX);
            OffsetYBox.Text = FormatCoordinate(_settings.VerticalOffsetY);
        }
        finally
        {
            _synchronizingOffsets = false;
        }

        UpdatePresetSelection();
    }

    private void UpdatePresetSelection()
    {
        if (!TryGetSelectedDisplay(out var screen))
        {
            return;
        }

        var (monitorBounds, usableBounds, dpiScaleX, dpiScaleY) = GetSelectedDisplayBounds(screen);
        var (width, height) = _app.GetOverlaySize();
        var matchingPreset = Enum.GetValues<OverlayPosition>().FirstOrDefault(preset =>
        {
            var offsets = OverlayPositionCalculator.GetPresetOffsets(
                monitorBounds,
                usableBounds,
                dpiScaleX,
                dpiScaleY,
                width,
                height,
                preset);
            return Math.Abs(offsets.X - _settings.HorizontalOffsetX) < 0.5 &&
                   Math.Abs(offsets.Y - _settings.VerticalOffsetY) < 0.5;
        });
        var hasMatch = Enum.GetValues<OverlayPosition>().Any(preset =>
        {
            var offsets = OverlayPositionCalculator.GetPresetOffsets(
                monitorBounds,
                usableBounds,
                dpiScaleX,
                dpiScaleY,
                width,
                height,
                preset);
            return Math.Abs(offsets.X - _settings.HorizontalOffsetX) < 0.5 &&
                   Math.Abs(offsets.Y - _settings.VerticalOffsetY) < 0.5;
        });

        _updatingPresetSelection = true;
        try
        {
            PositionCombo.SelectedValue = hasMatch ? matchingPreset.ToString() : null;
        }
        finally
        {
            _updatingPresetSelection = false;
        }
    }

    private void ApplySettings()
    {
        if (!_initialized ||
            !TryReadColor(BackgroundColorBox, out var backgroundColor) ||
            !TryReadColor(TextColorBox, out var textColor) ||
            !TryReadColor(AccentColorBox, out var accentColor) ||
            !TryReadColor(SecondaryAccentColorBox, out var secondaryColor) ||
            !TryParseCoordinate(OffsetXBox.Text, OffsetXSlider.Minimum, OffsetXSlider.Maximum, out var offsetX) ||
            !TryParseCoordinate(OffsetYBox.Text, OffsetYSlider.Minimum, OffsetYSlider.Maximum, out var offsetY))
        {
            return;
        }

        var startWithWindows = StartupCheck.IsChecked == true;
        if (startWithWindows != _settings.StartWithWindows)
        {
            try
            {
                _app.SetStartWithWindows(startWithWindows);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
            {
                System.Diagnostics.Trace.WriteLine($"Unable to update Termo startup registration: {exception}");
                StartupCheck.IsChecked = _settings.StartWithWindows;
            }
        }

        _settings.AlwaysOnTop = TopmostCheck.IsChecked == true;
        _settings.IsLocked = LockedCheck.IsChecked == true;
        _settings.ClickThrough = ClickThroughCheck.IsChecked == true;
        _settings.IsOverlayVisible = StartVisibleCheck.IsChecked == true;
        _settings.BackgroundEnabled = BackgroundEnabledCheck.IsChecked == true;
        _settings.BackgroundColor = backgroundColor;
        _settings.BackgroundOpacity = BackgroundOpacitySlider.Value;
        _settings.Opacity = OverlayOpacitySlider.Value;
        _settings.TextColor = textColor;
        _settings.AccentColor = accentColor;
        _settings.SecondaryAccentColor = secondaryColor;
        _settings.FontFamily = FontFamilyCombo.SelectedValue?.ToString() ?? "Segoe UI";
        _settings.FontSize = FontSizeSlider.Value;
        _settings.TextReadability = ReadabilityCombo.SelectedValue?.ToString() ?? "Normal";
        _settings.Theme = ThemeCombo.SelectedValue?.ToString() ?? "System";
        _settings.ShowCpuTemperature = CpuTemperatureCheck.IsChecked == true;
        _settings.ShowGpuTemperature = GpuTemperatureCheck.IsChecked == true;
        _settings.ShowCpuGraph = CpuGraphCheck.IsChecked == true;
        _settings.ShowGpuGraph = GpuGraphCheck.IsChecked == true;
        _settings.MetricSpacing = MetricSpacingSlider.Value;
        _settings.OverlayPadding = OverlayPaddingSlider.Value;
        _settings.HorizontalOffsetX = offsetX;
        _settings.VerticalOffsetY = offsetY;
        _settings.MonitorDeviceName = (MonitorCombo.SelectedItem as MonitorOption)?.DeviceName ?? string.Empty;
        _settings.EdgeReference = EdgeReferenceCombo.SelectedIndex == 0
            ? OverlayEdgeReference.WorkArea
            : OverlayEdgeReference.FullScreen;
        _settings.PositionCoordinatesVersion = 1;

        if (IntervalCombo.SelectedItem is ComboBoxItem interval &&
            int.TryParse(interval.Tag?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
        {
            _settings.RefreshIntervalMs = milliseconds;
        }

        if (PositionCombo.SelectedValue is string positionName &&
            Enum.TryParse<OverlayPosition>(positionName, out var positionPreset))
        {
            _settings.Position = positionPreset;
        }

        _settings.Validate();
        PositionControlsEnabled = !_settings.IsLocked;
        _app.ApplySettings(_settings);
        UpdateOffsetRange();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private static bool TryReadColor(TextBox textBox, out string value)
    {
        value = textBox.Text.Trim();
        if (value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit))
        {
            value = value.ToUpperInvariant();
            textBox.ToolTip = null;
            return true;
        }

        textBox.ToolTip = "Enter a six-digit hex color such as #4DE3D2.";
        textBox.Focus();
        return false;
    }

    private static bool TryParseCoordinate(string text, double minimum, double maximum, out double value)
    {
        var valid = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                    double.IsFinite(value) &&
                    value >= minimum &&
                    value <= maximum;
        if (valid)
        {
            value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        return valid;
    }

    private static string FormatCoordinate(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);

    private bool TryGetSelectedDisplay(out Forms.Screen screen)
    {
        var deviceName = (MonitorCombo.SelectedItem as MonitorOption)?.DeviceName;
        screen = Forms.Screen.AllScreens.FirstOrDefault(candidate =>
                     candidate.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                 ?? Forms.Screen.PrimaryScreen
                 ?? Forms.Screen.AllScreens.FirstOrDefault()!;
        return screen is not null;
    }

    private (PixelBounds Monitor, PixelBounds Usable, double ScaleX, double ScaleY) GetSelectedDisplayBounds(
        Forms.Screen screen)
    {
        var fullBounds = MainWindow.ToPixelBounds(screen.Bounds);
        var workBounds = MainWindow.ToPixelBounds(screen.WorkingArea);
        var usableBounds = OverlayPositionCalculator.SelectEdgeBounds(
            fullBounds,
            workBounds,
            EdgeReferenceCombo.SelectedIndex == 1
                ? OverlayEdgeReference.FullScreen
                : OverlayEdgeReference.WorkArea);
        var (scaleX, scaleY) = MainWindow.GetScale(screen);
        return (fullBounds, usableBounds, scaleX, scaleY);
    }

    private static List<MonitorOption> GetMonitorOptions() =>
        Forms.Screen.AllScreens
            .Select(screen => new MonitorOption(
                screen.DeviceName,
                $"{screen.DeviceName} {(screen.Primary ? "(Primary)" : string.Empty)}"))
            .ToList();

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var selectedDevice = (MonitorCombo.SelectedItem as MonitorOption)?.DeviceName;
            MonitorCombo.ItemsSource = GetMonitorOptions();
            MonitorCombo.SelectedItem = MonitorCombo.Items
                .Cast<MonitorOption>()
                .FirstOrDefault(item => item.DeviceName.Equals(selectedDevice, StringComparison.OrdinalIgnoreCase))
                ?? MonitorCombo.Items.Cast<MonitorOption>().FirstOrDefault(item => item.Name.Contains("(Primary)"));
            if (_initialized)
            {
                UpdateOffsetRange();
                ApplySettings();
            }
        }));
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var startupEnabled = _settings.StartWithWindows;
        var defaults = new OverlaySettings { StartWithWindows = startupEnabled };
        CopySettings(defaults, _settings);
        LoadSettings();
        ApplySettings();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        _saveTimer.Stop();
        _app.SaveSettings();
        Hide();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _saveTimer.Stop();
        if (!_allowClose)
        {
            _app.SaveSettings();
            e.Cancel = true;
            Hide();
        }
    }

    private static void CopySettings(OverlaySettings source, OverlaySettings destination)
    {
        destination.WindowLeft = source.WindowLeft;
        destination.WindowTop = source.WindowTop;
        destination.WindowWidth = source.WindowWidth;
        destination.WindowHeight = source.WindowHeight;
        destination.Opacity = source.Opacity;
        destination.BackgroundEnabled = source.BackgroundEnabled;
        destination.BackgroundColor = source.BackgroundColor;
        destination.BackgroundOpacity = source.BackgroundOpacity;
        destination.TextColor = source.TextColor;
        destination.AccentColor = source.AccentColor;
        destination.SecondaryAccentColor = source.SecondaryAccentColor;
        destination.FontFamily = source.FontFamily;
        destination.TextReadability = source.TextReadability;
        destination.OverlayPadding = source.OverlayPadding;
        destination.MetricSpacing = source.MetricSpacing;
        destination.ShowCpuTemperature = source.ShowCpuTemperature;
        destination.ShowGpuTemperature = source.ShowGpuTemperature;
        destination.ShowCpuGraph = source.ShowCpuGraph;
        destination.ShowGpuGraph = source.ShowGpuGraph;
        destination.FontSize = source.FontSize;
        destination.AlwaysOnTop = source.AlwaysOnTop;
        destination.ClickThrough = source.ClickThrough;
        destination.IsCompactMode = source.IsCompactMode;
        destination.IsOverlayVisible = source.IsOverlayVisible;
        destination.IsLocked = source.IsLocked;
        destination.StartMinimized = source.StartMinimized;
        destination.StartWithWindows = source.StartWithWindows;
        destination.RefreshIntervalMs = source.RefreshIntervalMs;
        destination.HistorySeconds = source.HistorySeconds;
        destination.HorizontalOffsetX = source.HorizontalOffsetX;
        destination.VerticalOffsetY = source.VerticalOffsetY;
        destination.EdgeReference = source.EdgeReference;
        destination.Position = source.Position;
        destination.Theme = source.Theme;
        destination.MonitorDeviceName = source.MonitorDeviceName;
        destination.PositionCoordinatesVersion = source.PositionCoordinatesVersion;
        destination.EdgeMargin = source.EdgeMargin;
    }

    private sealed record MonitorOption(string DeviceName, string Name);
}
