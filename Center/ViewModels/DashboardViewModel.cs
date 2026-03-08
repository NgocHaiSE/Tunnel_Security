using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty] private int _totalStations;
        [ObservableProperty] private int _activeStreams;
        [ObservableProperty] private double _systemHealthPercent;
        [ObservableProperty] private string _dataRateDisplay = "0 MB/s";

        public ObservableCollection<StationInfo> RecentStations { get; } = new();
        public ObservableCollection<LogEntry> LiveLogs { get; } = new();

        public void AddLog(LogEntry entry)
        {
            if (LiveLogs.Count > 200) LiveLogs.RemoveAt(0);
            LiveLogs.Add(entry);
        }

        public void UpdateStation(StationInfo station)
        {
            for (int i = 0; i < RecentStations.Count; i++)
            {
                if (RecentStations[i].StationCode == station.StationCode)
                {
                    RecentStations[i] = station;
                    return;
                }
            }
            if (RecentStations.Count < 5)
                RecentStations.Add(station);
        }
    }
}
