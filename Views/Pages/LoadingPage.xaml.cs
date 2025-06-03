namespace shlauncher.Views.Pages
{
    public partial class LoadingPage : INavigableView<ViewModels.Pages.LoadingViewModel>
    {
        public ViewModels.Pages.LoadingViewModel ViewModel { get; }

        public LoadingPage(ViewModels.Pages.LoadingViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}