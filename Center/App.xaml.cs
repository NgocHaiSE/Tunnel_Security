using Microsoft.UI.Xaml;
using System;
using Esri.ArcGISRuntime;

namespace Center
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain();
        /// </summary>
        public App()
        {
            InitializeComponent();

            // Initialize ArcGIS Runtime
            // Get your FREE API key from: https://developers.arcgis.com/
            try
            {
                // OPTION 1: Use API key for ArcGIS basemaps (recommended)
                // Sign up at https://developers.arcgis.com/ and create API key
                // Make sure to enable "Basemap styles service" when creating key
                // ArcGISRuntimeEnvironment.ApiKey = "PASTE_YOUR_API_KEY_HERE";

                // OPTION 2: Use OpenStreetMap (FREE, no API key needed)
                ArcGISRuntimeEnvironment.ApiKey = ""; // Empty = use OSM

                System.Diagnostics.Debug.WriteLine("ArcGIS Runtime initialized for Center");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ArcGIS initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
