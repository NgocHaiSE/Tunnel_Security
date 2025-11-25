using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Station.Services;
using Station.Views;

namespace Station
{
    public sealed partial class MainWindow : Window
    {
        private readonly StationConfigService _configService;
        private readonly ThemeService _themeService;
        private readonly Dictionary<Type, Window> _openWindows = new();

        public MainWindow()
        {
            InitializeComponent();
            _configService = new StationConfigService();
            _themeService = ThemeService.Instance;

            Title = "Trạm Nghĩa Đô - Hệ thống giám sát xâm nhập";

            // Set window to maximized for 4K dashboard
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1920, 1080));

            // Subscribe to theme changes
            _themeService.ThemeChanged += OnThemeChanged;

            // Apply current theme (default to Dark for 4K monitoring)
            _themeService.SetTheme(ElementTheme.Dark);
            ApplyTheme(ElementTheme.Dark);

            // Load station info
            _ = InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                // Load station info if configured
                var config = await _configService.GetConfigAsync();
                if (config != null)
                {
                    Title = $"{config.StationName} - Hệ thống giám sát xâm nhập";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            // Navigate to MonitoringDashboard in the main frame
            MonitoringFrame.Navigate(typeof(MonitoringDashboardPage));
        }

        /// <summary>
        /// Reload station configuration (called after saving new config)
        /// </summary>
        public async System.Threading.Tasks.Task ReloadStationConfigAsync()
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                if (config != null)
                {
                    Title = $"{config.StationName} - Hệ thống giám sát xâm nhập";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading config: {ex.Message}");
            }
        }

        private void OnThemeChanged(object? sender, ElementTheme theme)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ApplyTheme(theme);
            });
        }

        private void ApplyTheme(ElementTheme theme)
        {
            // Apply theme to the root content
            if (Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }

            // Apply theme to all open windows
            foreach (var window in _openWindows.Values)
            {
                if (window.Content is FrameworkElement element)
                {
                    element.RequestedTheme = theme;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Theme changed to: {theme}");
        }

        public void OpenPageInNewWindow<TPage>(string title) where TPage : Page, new()
        {
            var pageType = typeof(TPage);

            // Check if window is already open
            if (_openWindows.ContainsKey(pageType))
            {
                // Activate existing window
                _openWindows[pageType].Activate();
                return;
            }

            // Create new window
            var newWindow = new Window
            {
                Title = $"{title} - Trạm Nghĩa Đô",
                SystemBackdrop = new MicaBackdrop()
            };

            // Create frame with the page
            var frame = new Frame
            {
                Background = Application.Current.Resources["BackgroundSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
            };
            
            frame.Navigate(pageType);
            newWindow.Content = frame;

            // Apply current theme
            if (frame is FrameworkElement element)
            {
                element.RequestedTheme = _themeService.CurrentTheme;
            }

            // Set window size
            newWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // Handle window closed event
            newWindow.Closed += (s, e) =>
            {
                _openWindows.Remove(pageType);
                System.Diagnostics.Debug.WriteLine($"Closed window: {title}");
            };

            // Track the window
            _openWindows[pageType] = newWindow;

            // Activate the window
            newWindow.Activate();

            System.Diagnostics.Debug.WriteLine($"Opened new window: {title}");
        }
    }
}
