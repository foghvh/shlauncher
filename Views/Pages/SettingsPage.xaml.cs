namespace shlauncher.Views.Pages
{
    public partial class SettingsPage : INavigableView<ViewModels.Pages.SettingsViewModel>
    {
        public ViewModels.Pages.SettingsViewModel ViewModel { get; }

        public SettingsPage(ViewModels.Pages.SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}