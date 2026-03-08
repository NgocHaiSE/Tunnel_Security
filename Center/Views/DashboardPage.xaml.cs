using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
            LoadMockData();
        }

        private void LoadMockData()
        {
            TotalStationsText.Text = "12";
            OnlineCountText.Text = "10 online";
            ActiveAlertsText.Text = "3";
            CriticalCountText.Text = "1 critical";
            SystemHealthText.Text = "98.2%";
            DataRateText.Text = "842 MB/s";

            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-001", StationName = "Trạm Hầm Thủ Thiêm",
                Area = "TP.HCM", Status = StationStatus.Online,
                LatencyMs = 12, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-5)
            });
            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-002", StationName = "Trạm Hầm Bình Điền",
                Area = "TP.HCM", Status = StationStatus.Warning,
                LatencyMs = 142, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-15)
            });
            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-003", StationName = "Trạm Cầu Cần Thơ",
                Area = "Cần Thơ", Status = StationStatus.Online,
                LatencyMs = 22, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-3)
            });
        }

        private void ViewAllStations_Click(object s, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StationsPage));
        }
    }
}
