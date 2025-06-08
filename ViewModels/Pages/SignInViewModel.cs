using System.Threading.Tasks;
using System.Diagnostics;
using shlauncher.Models; // Para Profile
using Supabase.Gotrue; // Para Session

namespace shlauncher.ViewModels.Pages
{
    public partial class SignInViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly CurrentUserSessionService _sessionService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        [ObservableProperty]
        private string? _email; // Cambiado de Username a Email para el login

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
            // La lógica de GetRememberedUserSessionAsync ya es llamada en App.OnStartup
            // Aquí podríamos pre-rellenar el email si _sessionService.CurrentUser.Email existe
            // o si Properties.Settings.Default.RememberedUsername (que ahora sería email) existe.
            // Por simplicidad, dejaremos que App.OnStartup maneje la restauración de sesión.
            // Si no hay sesión restaurada, esta página se mostrará y el usuario ingresará datos.
            // Si el usuario fue recordado pero la sesión expiró, podríamos querer pre-rellenar el email.
            var rememberedLoginForUi = Properties.Settings.Default.RememberedUsername; // Esto ahora contendría el login/email
            if (!string.IsNullOrEmpty(rememberedLoginForUi) && Properties.Settings.Default.RememberedToken != "")
            {
                Email = rememberedLoginForUi; // Asumimos que el "username" recordado es el email
                RememberMe = true;
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await ShowMessageAsync("Login Failed", "Please enter both email and password.");
                return;
            }

            _mainWindowViewModel.IsGlobalLoading = true;
            var (success, sessionData, profileData, errorMessage) = await _authService.LoginAsync(Email, Password);

            if (success && sessionData?.User != null && profileData != null)
            {
                _sessionService.SetCurrentUser(profileData, sessionData);
                if (RememberMe && sessionData.User.Id != null && profileData.Login != null && sessionData.AccessToken != null && sessionData.RefreshToken != null)
                {
                    _authService.RememberUser(sessionData.AccessToken, sessionData.RefreshToken, sessionData.User.Id, profileData.Login);
                }
                else
                {
                    _authService.ClearRememberedUser();
                }
                _navigationService.Navigate(typeof(Views.Pages.MainLauncherPage)); // O LoadingPage si prefieres
            }
            else
            {
                await ShowMessageAsync("Login Failed", errorMessage ?? "Login failed due to an unknown error.");
            }
            _mainWindowViewModel.IsGlobalLoading = false;
        }

        [RelayCommand]
        private void NavigateToSignUp() // Cambiado de Register a NavigateToSignUp
        {
            _navigationService.Navigate(typeof(Views.Pages.SignUpPage));
        }

        private async Task ShowMessageAsync(string title, string content)
        {
            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK"
            };
            await msgBox.ShowDialogAsync();
        }

        public override Task OnNavigatedToAsync()
        {
            Password = null; // Limpiar contraseña al navegar a esta página
            // La carga de usuario recordado es mejor en el constructor o App.xaml.cs para que ocurra una vez
            return base.OnNavigatedToAsync();
        }
    }
}
