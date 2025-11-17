using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Station.ViewModels;

namespace Station.Views
{
    public sealed partial class AlertsPage : Page
    {
        public AlertsViewModel ViewModel { get; }

        public AlertsPage()
        {
            this.InitializeComponent();
            ViewModel = new AlertsViewModel();
        }

        private void FilterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // ?óng flyout khi click vào item
            if (sender is ListView listView)
            {
                // Tìm flyout parent và ?óng nó
                if (DeviceFlyout.IsOpen)
                {
                    DeviceFlyout.Hide();
                }
                else if (SeverityFlyout.IsOpen)
                {
                    SeverityFlyout.Hide();
                }
                else if (StatusFlyout.IsOpen)
                {
                    StatusFlyout.Hide();
                }
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This event handler ensures the ComboBox updates are processed
            // The actual state update is handled by the ViewModel binding
        }
    }
}
