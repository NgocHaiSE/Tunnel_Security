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

        /// <summary>
        /// Handles node expand/collapse button click
        /// </summary>
        private void NodeExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Find the parent Border (Node Card)
                var parent = button.Parent;
                while (parent != null && parent is not Border)
                {
                    parent = VisualTreeHelper.GetParent(parent) as DependencyObject;
                }

                if (parent is Border nodeBorder)
                {
                    // Find the SensorsPanel and ExpandIcon
                    var sensorsPanel = FindChildByName<StackPanel>(nodeBorder, "SensorsPanel");
                    var expandIcon = FindChildByName<FontIcon>(button, "ExpandIcon");

                    if (sensorsPanel != null && expandIcon != null)
                    {
                        // Toggle visibility
                        if (sensorsPanel.Visibility == Visibility.Collapsed)
                        {
                            sensorsPanel.Visibility = Visibility.Visible;
                            expandIcon.Glyph = "\uE70E"; // ChevronUp
                        }
                        else
                        {
                            sensorsPanel.Visibility = Visibility.Collapsed;
                            expandIcon.Glyph = "\uE70D"; // ChevronDown
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Highlights sensor card on mouse enter
        /// </summary>
        private void SensorCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["PrimaryMediumBrush"];
                border.BorderThickness = new Thickness(2);
                border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundAccentBrush"];
            }
        }

        /// <summary>
        /// Restores sensor card on mouse leave
        /// </summary>
        private void SensorCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderLightBrush"];
                border.BorderThickness = new Thickness(1);
                border.Background = (SolidColorBrush)Application.Current.Resources["BackgroundSecondaryBrush"];
            }
        }

        /// <summary>
        /// Opens sensor detail dialog when sensor card is tapped
        /// </summary>
        private void SensorCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SensorItemViewModel sensor)
            {
                ViewModel.SelectedSensor = sensor;
            }
        }

        /// <summary>
        /// Helper method to find a child element by name
        /// </summary>
        private T FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }

                var result = FindChildByName<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}