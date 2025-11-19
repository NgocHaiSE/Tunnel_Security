using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Station.Services;
using Station.Views;

namespace Station
{
    public sealed partial class MainWindow : Window
    {
        private readonly StationConfigService _configService;
        private readonly ThemeService _themeService;

        public MainWindow()
        {
            InitializeComponent();
            _configService = new StationConfigService();
            _themeService = ThemeService.Instance;

            Title = "Trạm Nghĩa Đô - Hệ thống giám sát xâm nhập";

            // Set window size
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // Always show sidebar
            NavView.IsPaneVisible = true;
            NavView.IsPaneOpen = true;

            // Subscribe to theme changes
            _themeService.ThemeChanged += OnThemeChanged;

            // Apply current theme
            ApplyTheme(_themeService.CurrentTheme);

            // Load station info and navigate
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
                    StationNameText.Text = $"{config.StationCode} - {config.StationName}";
                    StationAreaText.Text = $"Khu vực: {config.Area ?? "Chưa xác định"}";
                    Title = $"{config.StationName} - Hệ thống giám sát xâm nhập";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            // Always navigate to dashboard
            ContentFrame.Navigate(typeof(DashboardPage));
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
                    StationNameText.Text = $"{config.StationCode} - {config.StationName}";
                    StationAreaText.Text = $"Khu vực: {config.Area ?? "Chưa xác định"}";
                    Title = $"{config.StationName} - Hệ thống giám sát xâm nhập";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading config: {ex.Message}");
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _themeService.ToggleTheme();
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

            // Update theme toggle icon
            if (theme == ElementTheme.Dark)
            {
                // In dark mode, show sun icon (click to go light)
                MoonIcon.Visibility = Visibility.Collapsed;
                SunIcon.Visibility = Visibility.Visible;
            }
            else
            {
                // In light mode, show moon icon (click to go dark)
                MoonIcon.Visibility = Visibility.Visible;
                SunIcon.Visibility = Visibility.Collapsed;
            }

            System.Diagnostics.Debug.WriteLine($"Theme changed to: {theme}");
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(ConfigurationPage));
                return;
            }

            if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                var tag = selectedItem.Tag?.ToString();

                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;

                    case "MonitoringDashboard":
                        ContentFrame.Navigate(typeof(MonitoringDashboardPage));
                        break;

                    case "Devices":
                        ContentFrame.Navigate(typeof(DevicesPage));
                        break;

                    case "LiveVideo":
                        ContentFrame.Navigate(typeof(LiveVideoPage));
                        break;

                    case "Alerts":
                        ContentFrame.Navigate(typeof(AlertsPage));
                        break;

                    case "Data":
                        ContentFrame.Navigate(typeof(DataPage));
                        break;

                    case "Map":
                        ContentFrame.Navigate(typeof(MapPage));
                        break;

                    case "Configuration":
                        ContentFrame.Navigate(typeof(ConfigurationPage));
                        break;

                    default:
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                }
            }
        }

        /// <summary>
        /// Navigate programmatically to a specific page
        /// </summary>
        public void NavigateTo(Type pageType)
        {
            ContentFrame.Navigate(pageType);

            // Update NavigationView selection
            var menuItem = NavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == pageType.Name.Replace("Page", ""));

            if (menuItem != null)
            {
                NavView.SelectedItem = menuItem;
            }
        }
    }
}
