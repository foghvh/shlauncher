namespace shlauncher.Views.Pages
{
    public partial class SignInPage : INavigableView<ViewModels.Pages.SignInViewModel>
    {
        public ViewModels.Pages.SignInViewModel ViewModel { get; }

        public SignInPage(ViewModels.Pages.SignInViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                ViewModel.Password = passwordBox.Password;
            }
        }
    }
}