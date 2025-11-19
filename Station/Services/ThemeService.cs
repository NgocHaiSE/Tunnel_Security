using Microsoft.UI.Xaml;
using System;

namespace Station.Services;

/// <summary>
/// Service to manage application theme switching between Light and Dark modes
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    private ElementTheme _currentTheme = ElementTheme.Light;

    public static ThemeService Instance => _instance ??= new ThemeService();

    public event EventHandler<ElementTheme>? ThemeChanged;

    private ThemeService()
    {
        // Load saved theme preference
        LoadThemePreference();
    }

    public ElementTheme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                SaveThemePreference();
                ThemeChanged?.Invoke(this, _currentTheme);
            }
        }
    }

    /// <summary>
    /// Toggle between Light and Dark theme
    /// </summary>
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ElementTheme.Light
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    /// <summary>
    /// Set specific theme
    /// </summary>
    public void SetTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
    }

    /// <summary>
    /// Check if current theme is dark
    /// </summary>
    public bool IsDarkTheme => CurrentTheme == ElementTheme.Dark;

    private void LoadThemePreference()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AppTheme", out var themeValue))
            {
                if (Enum.TryParse<ElementTheme>(themeValue.ToString(), out var theme))
                {
                    _currentTheme = theme;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
        }
    }

    private void SaveThemePreference()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["AppTheme"] = CurrentTheme.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
        }
    }
}
