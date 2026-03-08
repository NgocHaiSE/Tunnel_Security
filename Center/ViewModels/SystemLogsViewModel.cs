using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class SystemLogsViewModel : ObservableObject
    {
        [ObservableProperty] private string _levelFilter = "All";
        [ObservableProperty] private string _keyword = string.Empty;

        public ObservableCollection<LogEntry> Logs { get; } = new();
        public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

        partial void OnLevelFilterChanged(string value) => ApplyFilter();
        partial void OnKeywordChanged(string value) => ApplyFilter();

        public void AddLog(LogEntry entry)
        {
            Logs.Insert(0, entry);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredLogs.Clear();
            foreach (var l in Logs)
            {
                bool matchLevel = LevelFilter == "All" || l.Level.ToString() == LevelFilter;
                bool matchKey = string.IsNullOrEmpty(Keyword) ||
                    l.Message.Contains(Keyword, System.StringComparison.OrdinalIgnoreCase) ||
                    l.Source.Contains(Keyword, System.StringComparison.OrdinalIgnoreCase);
                if (matchLevel && matchKey) FilteredLogs.Add(l);
            }
        }
    }
}
