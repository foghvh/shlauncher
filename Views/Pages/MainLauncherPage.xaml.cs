namespace shlauncher.Views.Pages
{
    public partial class MainLauncherPage : INavigableView<ViewModels.Pages.MainLauncherViewModel>
    {
        public ViewModels.Pages.MainLauncherViewModel ViewModel { get; }

        public MainLauncherPage(ViewModels.Pages.MainLauncherViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}