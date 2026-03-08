using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Center.Views
{
    public sealed partial class StationsPage : Page
    {
        public StationsViewModel ViewModel { get; } = new();

        public StationsPage()
        {
            InitializeComponent();
            ViewModel.LoadStations(GetMockStations());
        }

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StatusFilter = (sender as Button)?.Tag?.ToString() ?? "All";
        }

        private static List<StationInfo> GetMockStations() => new()
        {
            new() { StationCode="TRM-001", StationName="Hầm Thủ Thiêm", Area="TP.HCM",
                    Status=StationStatus.Online, LatencyMs=12, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-5) },
            new() { StationCode="TRM-002", StationName="Hầm Bình Điền", Area="TP.HCM",
                    Status=StationStatus.Warning, LatencyMs=142, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-20) },
            new() { StationCode="TRM-003", StationName="Cầu Cần Thơ", Area="Cần Thơ",
                    Status=StationStatus.Online, LatencyMs=22, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-3) },
            new() { StationCode="TRM-004", StationName="Hầm Đèo Cả", Area="Phú Yên",
                    Status=StationStatus.Offline, LatencyMs=0, LastHeartbeat=DateTimeOffset.Now.AddMinutes(-30) },
        };
    }
}
