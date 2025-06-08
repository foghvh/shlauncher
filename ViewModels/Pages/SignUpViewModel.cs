using System.Threading.Tasks;
using System.Diagnostics;

namespace shlauncher.ViewModels.Pages
{
    public partial class SignUpViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        [ObservableProperty]
        private string? _login;

        [ObservableProperty]
        private string? _email;

        private string? _password;
        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string? _confirmPassword;
        public string? ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public SignUpViewModel(INavigationService navigationService, AuthService authService, MainWindowViewModel mainWindowViewModel)
        {
            _navigationService = navigationService;
            _authService = authService;
            _mainWindowViewModel = mainWindowViewModel;
            PageTitle = "Sign Up - SHLauncher";
        }

        [RelayCommand]
        private async Task SignUp()
        {
            if (string.IsNullOrWhiteSpace(Login) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await ShowMessageAsync("Sign Up Failed", "All fields are required.");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await ShowMessageAsync("Sign Up Failed", "Passwords do not match.");
                return;
            }

            _mainWindowViewModel.IsGlobalLoading = true;
            var (success, user, profile, message) = await _authService.RegisterAsync(Email, Password, Login);
            _mainWindowViewModel.IsGlobalLoading = false;

            if (success)
            {
                await ShowMessageAsync("Registration Successful", message ?? "Please check your email to confirm your account.", true);
                _navigationService.Navigate(typeof(Views.Pages.SignInPage));
            }
            else
            {
                await ShowMessageAsync("Sign Up Failed", message ?? "An unknown error occurred during registration.");
            }
        }

        [RelayCommand]
        private void NavigateToSignIn()
        {
            _navigationService.Navigate(typeof(Views.Pages.SignInPage));
        }

        private async Task ShowMessageAsync(string title, string content, bool primaryButtonOk = false)
        {
            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = content,
                CloseButtonText = primaryButtonOk ? "" : "OK",
                PrimaryButtonText = primaryButtonOk ? "OK" : ""
            };
            await msgBox.ShowDialogAsync();
        }

        public override Task OnNavigatedToAsync()
        {
            Login = null;
            Email = null;
            Password = null;
            ConfirmPassword = null;
            return base.OnNavigatedToAsync();
        }
    }
}
