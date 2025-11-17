using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Esri.ArcGISRuntime;

namespace Station
{
    public partial class App : Application
    {
        public Window? m_window { get; private set; }

        public App()
        {
            InitializeComponent();

            try
            {
                // Set API key (optional for basic basemaps, but recommended)
                // Get your free API key at: https://developers.arcgis.com/
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "AAPTxy8BH1VEsoebNVZXo8HurBHPuRuPZica8Rhed5m-n7B5cMrNHa4NpHsBxhhNvfjc7bhhLR2kIHFMyPSkBk131jRg65s7pRbWdgm06q2S2OExkLc7suvcbtiXBHjcdAhYdeaNUpb1cIxiUnRZmwvs8uF_klAGOU2D_zrOwWW7mn0Jr1T41UtpPVr3_oz9zZmgbaG_YEjqazp4EuqS56m5eKM5-JYPIKaLNMkuR8Pv7F4.AT1_OAgey3rQ"; // Leave empty for now - public basemaps will still work
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ArcGIS initialization error: {ex.Message}");
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
