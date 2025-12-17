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