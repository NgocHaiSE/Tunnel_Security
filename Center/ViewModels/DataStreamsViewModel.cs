using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class DataStreamsViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isPaused = false;
        [ObservableProperty] private string _stationFilter = "All";

        public ObservableCollection<LogEntry> Entries { get; } = new();

        public void AddEntry(LogEntry entry)
        {
            if (IsPaused) return;
            if (StationFilter != "All" && !entry.Source.Contains(StationFilter)) return;
            if (Entries.Count > 500) Entries.RemoveAt(0);
            Entries.Add(entry);
        }

        public void Clear() => Entries.Clear();
    }
}
