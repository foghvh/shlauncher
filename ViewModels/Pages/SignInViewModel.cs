using System.Threading.Tasks;
using System.Diagnostics;

namespace shlauncher.ViewModels.Pages
{
    public partial class SignInViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly CurrentUserSessionService _sessionService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        [ObservableProperty]
        private string? _username;

        private string? _password;
        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        [ObservableProperty]
        private bool _rememberMe;

        public SignInViewModel(INavigationService navigationService, AuthService authService, CurrentUserSessionService sessionService, MainWindowViewModel mainWindowViewModel)
        {
            _navigationService = navigationService;
            _authService = authService;
            _sessionService = sessionService;
            _mainWindowViewModel = mainWindowViewModel;
            PageTitle = "Sign In - SHLauncher";
            LoadRememberedUser();
        }

        private void LoadRememberedUser()
        {
            if (_sessionService.IsUserLoggedIn && _sessionService.CurrentUser != null)
            {
                Username = _sessionService.CurrentUser.Login;
                RememberMe = true;
            }
            else
            {
                var (token, rememberedUsername) = _authService.GetRememberedUser();
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(rememberedUsername))
                {
                    Username = rememberedUsername;
                    RememberMe = true;
                }
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Login Failed",
                    Content = "Please enter both username and password.",
                    CloseButtonText = "OK",
                    PrimaryButtonText = "" // No primary button needed if close is OK
                };
                await msgBox.ShowDialogAsync();
                return;
            }

            _mainWindowViewModel.IsGlobalLoading = true;
            var (success, token, userData, errorMessage) = await _authService.LoginAsync(Username, Password);

            if (success && token != null && userData != null)
            {
                _sessionService.SetCurrentUser(userData, token);
                if (RememberMe)
                {
                    _authService.RememberUser(token, userData.Login!);
                }
                else
                {
                    _authService.ClearRememberedUser();
                }
                _navigationService.Navigate(typeof(Views.Pages.LoadingPage));
            }
            else
            {
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Login Failed",
                    Content = errorMessage ?? "Login failed due to an unknown error.",
                    CloseButtonText = "OK"
                };
                await msgBox.ShowDialogAsync();
            }
            _mainWindowViewModel.IsGlobalLoading = false;
        }

        [RelayCommand]
        private void Register()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://skinhunterv2.vercel.app") { UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error opening registration link: {ex.Message}");
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = $"Could not open registration page: {ex.Message}",
                    CloseButtonText = "OK"
                };
                _ = msgBox.ShowDialogAsync(); // Fire and forget if not awaiting result
            }
        }

        public override Task OnNavigatedToAsync()
        {
            Password = null;
            LoadRememberedUser();
            return base.OnNavigatedToAsync();
        }
    }
}