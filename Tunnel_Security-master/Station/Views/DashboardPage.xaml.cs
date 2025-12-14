using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;

namespace Station.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
  {
            this.InitializeComponent();
            ViewModel = new DashboardViewModel();
        }
    }
}
