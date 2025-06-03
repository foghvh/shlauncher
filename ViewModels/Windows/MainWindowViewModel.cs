using System.Collections.ObjectModel;

namespace shlauncher.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "Skin-hunter";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new();

        [ObservableProperty]
        private bool _isGlobalLoading = false;
    }
}