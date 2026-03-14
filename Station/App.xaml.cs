using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Station.Services;
using Station.Views;
using System.Threading.Tasks;

namespace Station
{
    public partial class App : Application
    {
        public Window? m_window { get; private set; }
        private SimulationApiServer? _simServer;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Start mock data simulation
            MockDataService.Instance.Start();

            // Start web API server
            _simServer = new SimulationApiServer();
            Task.Run(() => _simServer.StartAsync());

            m_window = new Window();

            Frame rootFrame = new Frame();
            rootFrame.Navigate(typeof(LoginPage));

            m_window.Content = rootFrame;
            m_window.Activate();
        }
    }
}