using Center.ViewModels;
using Center.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace Center
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();
        private Button? _activeNav;

        public MainWindow()
        {
            InitializeComponent();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 960));

            // Start clock
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => ClockText.Text = DateTimeOffset.Now.ToString("HH:mm:ss");
            timer.Start();
            ClockText.Text = DateTimeOffset.Now.ToString("HH:mm:ss");

            // Navigate to Dashboard on load
            ContentFrame.Loaded += (_, _) => SetActivePage(NavDashboard, "Dashboard", "\uE80F", typeof(DashboardPage));
        }

        private void SetActivePage(Button nav, string title, string glyph, Type pageType)
        {
            // Reset previous active
            if (_activeNav != null)
            {
                _activeNav.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                _activeNav.Foreground = (SolidColorBrush)Application.Current.Resources["TextMutedBrush"];
            }

            // Set new active
            _activeNav = nav;
            nav.Background = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"];
            nav.Foreground = new SolidColorBrush(Colors.White);

            PageTitleText.Text = title;
            HeaderIcon.Glyph = glyph;
            ContentFrame.Navigate(pageType);
        }

        private void NavDashboard_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavDashboard, "Dashboard", "\uE80F", typeof(DashboardPage));

        private void NavStations_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavStations, "Stations", "\uE968", typeof(StationsPage));

        private void NavAlerts_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavAlerts, "Alerts", "\uE7BA", typeof(AlertsPage));

        private void NavDataStreams_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavDataStreams, "Data Streams", "\uE9D9", typeof(DataStreamsPage));

        private void NavSystemLogs_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavSystemLogs, "System Logs", "\uE9F9", typeof(SystemLogsPage));

        private void AddStation_Click(object s, RoutedEventArgs e)
        {
            // TODO: open AddStationDialog
        }

        private void Notification_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavAlerts, "Alerts", "\uE7BA", typeof(AlertsPage));

        private void Settings_Click(object s, RoutedEventArgs e)
        {
            // TODO: settings flyout
        }

        public void SetAlertBadge(int count)
        {
            AlertBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            AlertBadgeText.Text = count > 99 ? "99+" : count.ToString();
            NotifDot.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
