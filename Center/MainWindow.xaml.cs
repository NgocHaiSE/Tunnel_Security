using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center
{
    /// <summary>
    /// Center window with ArcGIS Map for monitoring all stations
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            // Set window size for center monitoring
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 1000));


        }

    }
}
