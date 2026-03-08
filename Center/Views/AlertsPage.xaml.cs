using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center.Views
{
    public sealed partial class AlertsPage : Page
    {
        public AlertsViewModel ViewModel { get; } = new();

        public AlertsPage()
        {
            InitializeComponent();
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-002",
                Severity=AlertSeverity.Warning, Message="High latency detected (142ms)",
                Timestamp=DateTimeOffset.Now.AddMinutes(-2) });
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-004",
                Severity=AlertSeverity.Critical, Message="Station offline — heartbeat timeout",
                Timestamp=DateTimeOffset.Now.AddMinutes(-30) });
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-001",
                Severity=AlertSeverity.Info, Message="Data sync completed",
                Timestamp=DateTimeOffset.Now.AddMinutes(-1) });
        }

        private void SeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SeverityFilter = (sender as Button)?.Tag?.ToString() ?? "All";
        }
    }
}
