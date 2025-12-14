using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Station.ViewModels;

namespace Station.Views
{
    /// <summary>
    /// Devices management page with grid layout, filtering, and device actions menu
    /// </summary>
    public sealed partial class DevicesPage : Page
    {
        public DevicesViewModel ViewModel { get; }

        public DevicesPage()
        {
            this.InitializeComponent();
            ViewModel = new DevicesViewModel();
        }

        /// <summary>
        /// Closes the flyout when a filter item is clicked
        /// </summary>
        private void FilterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Close flyout when item is clicked
            if (sender is ListView listView)
            {
                // Find the parent flyout and close it
                var parent = listView.Parent;
                while (parent != null && parent is not FlyoutPresenter)
                {
                    parent = VisualTreeHelper.GetParent(parent) as DependencyObject;
                }

                if (parent is FlyoutPresenter presenter && presenter.Parent is Popup popup)
                {
                    popup.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Highlights device card border on mouse enter
        /// </summary>
        private void DeviceCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["PrimaryMediumBrush"];
                border.BorderThickness = new Thickness(2);
            }
        }

        /// <summary>
        /// Restores device card border on mouse leave
        /// </summary>
        private void DeviceCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderLightBrush"];
                border.BorderThickness = new Thickness(1);
            }
        }
    }
}
