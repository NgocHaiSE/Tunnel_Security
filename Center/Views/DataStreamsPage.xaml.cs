using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace Center.Views
{
    public sealed partial class DataStreamsPage : Page
    {
        public DataStreamsViewModel ViewModel { get; } = new();
        private DispatcherTimer? _mockTimer;

        public DataStreamsPage()
        {
            InitializeComponent();
            StartMockFeed();
        }

        private void StartMockFeed()
        {
            _mockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            var rng = new Random();
            string[] stations = { "TRM-001", "TRM-002", "TRM-003" };
            string[] msgs = {
                "Heartbeat acknowledged", "Data sync complete",
                "Sensor reading: temp=28.4C", "Radar scan nominal",
                "Alert threshold check passed", "Camera feed stable"
            };
            _mockTimer.Tick += (_, _) =>
            {
                ViewModel.AddEntry(new LogEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Level = LogLevel.Info,
                    Source = stations[rng.Next(stations.Length)],
                    Message = msgs[rng.Next(msgs.Length)]
                });
                LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.ExtentHeight);
            };
            _mockTimer.Start();
        }

        private void Pause_Click(object s, RoutedEventArgs e)
        {
            ViewModel.IsPaused = !ViewModel.IsPaused;
            (s as ToggleButton)!.Content = ViewModel.IsPaused ? "&#x25B6; Resume" : "&#x23F8; Pause";
        }

        private void Clear_Click(object s, RoutedEventArgs e) => ViewModel.Clear();

        private void StationFilter_Changed(object s, SelectionChangedEventArgs e)
        {
            var item = (s as ComboBox)?.SelectedItem?.ToString() ?? "All";
            ViewModel.StationFilter = item;
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _mockTimer?.Stop();
        }
    }
}
