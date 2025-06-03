namespace shlauncher.Views.Pages
{
    public partial class WelcomePage : INavigableView<ViewModels.Pages.WelcomeViewModel>
    {
        public ViewModels.Pages.WelcomeViewModel ViewModel { get; }

        public WelcomePage(ViewModels.Pages.WelcomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}