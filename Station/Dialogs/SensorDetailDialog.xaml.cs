using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;

namespace Station.Dialogs
{
    public sealed partial class SensorDetailDialog : ContentDialog
    {
        public SensorItemViewModel Sensor { get; }

        public SensorDetailDialog(SensorItemViewModel sensor)
        {
            this.InitializeComponent();
            Sensor = sensor;
        }
    }
}
