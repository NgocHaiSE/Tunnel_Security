using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class StationsViewModel : ObservableObject
    {
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusFilter = "All";

        public ObservableCollection<StationInfo> Stations { get; } = new();
        public ObservableCollection<StationInfo> FilteredStations { get; } = new();

        partial void OnSearchQueryChanged(string value) => ApplyFilter();
        partial void OnStatusFilterChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            FilteredStations.Clear();
            foreach (var s in Stations)
            {
                bool matchesSearch = string.IsNullOrEmpty(SearchQuery) ||
                    s.StationCode.Contains(SearchQuery, System.StringComparison.OrdinalIgnoreCase) ||
                    s.StationName.Contains(SearchQuery, System.StringComparison.OrdinalIgnoreCase);
                bool matchesStatus = StatusFilter == "All" || s.Status.ToString() == StatusFilter;
                if (matchesSearch && matchesStatus)
                    FilteredStations.Add(s);
            }
        }

        public void LoadStations(System.Collections.Generic.List<StationInfo> stations)
        {
            Stations.Clear();
            foreach (var s in stations) Stations.Add(s);
            ApplyFilter();
        }
    }
}
