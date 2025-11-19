using Microsoft.UI.Xaml;
using System;
using Esri.ArcGISRuntime;

namespace Center
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();

            try
            {
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "AAPTxy8BH1VEsoebNVZXo8HurBHPuRuPZica8Rhed5m-n7AFkMnLsrCwK3gDIGNQy02avEYsa1pMFxSHBelsSwL8uSTQLglhWVCVYKR0ohqXZDVqwOLyETQGepBvW6s9DKmlSDkhdyzKF5j_bzBrcTa0nXSg8hb4exXcd3yq7Jt91zJbeaI9UdqbOedgq7GxqkA20s_XUdyXKRfxFGws6VNKVTkxGK8ZH0qvhixUAVTa-Rc.AT1_OAgey3rQ";
                System.Diagnostics.Debug.WriteLine("ArcGIS Runtime initialized for Center");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ArcGIS initialization error: {ex.Message}");
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

    }
}
