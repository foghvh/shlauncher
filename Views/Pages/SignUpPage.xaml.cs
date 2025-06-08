namespace shlauncher.Views.Pages
{
    public partial class SignUpPage : INavigableView<ViewModels.Pages.SignUpViewModel>
    {
        public ViewModels.Pages.SignUpViewModel ViewModel { get; }

        public SignUpPage(ViewModels.Pages.SignUpViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
