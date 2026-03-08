using Microsoft.UI.Xaml;
using Station.Services;
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

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Start mock data simulation
            MockDataService.Instance.Start();

            // Start web API server on background thread
            _simServer = new SimulationApiServer();
            Task.Run(() => _simServer.StartAsync());

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
