using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Station.ViewModels;

namespace Station.Views
{
    public sealed partial class LiveVideoPage : Page
    {
        public LiveVideoViewModel ViewModel { get; }

        public LiveVideoPage()
        {
            this.InitializeComponent();
            ViewModel = new LiveVideoViewModel();
        }

        // Layout button click handlers
        private void Layout1x1_Click(object sender, RoutedEventArgs e)
        {
            ChangeGridLayout(800, 600);
        }

        private void Layout2x2_Click(object sender, RoutedEventArgs e)
        {
            ChangeGridLayout(420, 340);
        }

        private void Layout3x3_Click(object sender, RoutedEventArgs e)
        {
            ChangeGridLayout(300, 250);
        }

        private void Layout4x4_Click(object sender, RoutedEventArgs e)
        {
            ChangeGridLayout(240, 200);
        }

        private void ChangeGridLayout(double width, double height)
        {
            var itemsWrapGrid = FindItemsWrapGrid(CameraItemsControl);
            if (itemsWrapGrid != null)
            {
                itemsWrapGrid.ItemWidth = width;
                itemsWrapGrid.ItemHeight = height;
            }
        }

        private ItemsWrapGrid? FindItemsWrapGrid(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ItemsWrapGrid grid)
                    return grid;
                var found = FindItemsWrapGrid(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        // Camera selection handlers
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
            {
                camera.IsSelected = true;
            }
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
            {
                camera.IsSelected = false;
            }
        }
    }
}