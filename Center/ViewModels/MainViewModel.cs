using CommunityToolkit.Mvvm.ComponentModel;

namespace Center.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Dashboard";

        [ObservableProperty]
        private int _unreadAlertCount = 0;

        [ObservableProperty]
        private bool _hasUnreadAlerts = false;

        partial void OnUnreadAlertCountChanged(int value)
        {
            HasUnreadAlerts = value > 0;
        }
    }
}
