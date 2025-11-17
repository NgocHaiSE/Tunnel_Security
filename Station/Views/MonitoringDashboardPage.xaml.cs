using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;

namespace Station.Views
{
    public sealed partial class MonitoringDashboardPage : Page
    {
        public MonitoringDashboardViewModel ViewModel { get; }

        public MonitoringDashboardPage()
        {
            InitializeComponent();
            ViewModel = (MonitoringDashboardViewModel)DataContext;
        }
    }
}
