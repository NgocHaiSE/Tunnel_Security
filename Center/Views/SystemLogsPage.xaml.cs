using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text;
using Windows.Storage.Pickers;

namespace Center.Views
{
    public sealed partial class SystemLogsPage : Page
    {
        public SystemLogsViewModel ViewModel { get; } = new();

        public SystemLogsPage()
        {
            InitializeComponent();
            LoadMockLogs();
        }

        private void LoadMockLogs()
        {
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-1),  Level=LogLevel.Info,    Source="CenterBackend", Message="Station TRM-001 heartbeat received" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-5),  Level=LogLevel.Warning, Source="CenterBackend", Message="Station TRM-002 latency spike: 142ms" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-30), Level=LogLevel.Error,   Source="CenterBackend", Message="Station TRM-004 connection lost" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddHours(-1),    Level=LogLevel.Info,    Source="System",        Message="Scheduled integrity check completed" });
        }

        private void LevelFilter_Changed(object s, SelectionChangedEventArgs e)
        {
            ViewModel.LevelFilter = (LevelCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        }

        private async void Export_Click(object s, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("CSV", new[] { ".csv" });
            picker.SuggestedFileName = $"system-logs-{DateTime.Now:yyyy-MM-dd}";
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            var sb = new StringBuilder("Timestamp,Level,Source,Message\n");
            foreach (var log in ViewModel.Logs)
                sb.AppendLine($"{log.Timestamp},{log.Level},{log.Source},\"{log.Message}\"");
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        }
    }
}
