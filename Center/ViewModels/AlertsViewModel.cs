using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class AlertsViewModel : ObservableObject
    {
        [ObservableProperty] private string _severityFilter = "All";
        [ObservableProperty] private string _stationFilter = "All";

        public ObservableCollection<AlertEntry> Alerts { get; } = new();
        public ObservableCollection<AlertEntry> FilteredAlerts { get; } = new();

        public void AddAlert(AlertEntry alert)
        {
            Alerts.Insert(0, alert);
            ApplyFilter();
        }

        partial void OnSeverityFilterChanged(string value) => ApplyFilter();
        partial void OnStationFilterChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            FilteredAlerts.Clear();
            foreach (var a in Alerts)
            {
                bool matchSev = SeverityFilter == "All" || a.Severity.ToString() == SeverityFilter;
                bool matchSta = StationFilter == "All" || a.StationCode == StationFilter;
                if (matchSev && matchSta) FilteredAlerts.Add(a);
            }
        }
    }
}
