using System.Threading.Tasks;

namespace shlauncher.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        private readonly CurrentUserSessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly SupabaseService _supabaseService; // Añadir SupabaseService
        private readonly MainWindowViewModel _mainWindowViewModel;

        public SettingsViewModel(
            CurrentUserSessionService sessionService,
            INavigationService navigationService,
            AuthService authService,
            SupabaseService supabaseService,
            MainWindowViewModel mainWindowViewModel)
        {
            _sessionService = sessionService;
            _navigationService = navigationService;
            _authService = authService;
            _supabaseService = supabaseService;
            _mainWindowViewModel = mainWindowViewModel;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            _isInitialized = false; // Para que se reinicialice si volvemos
            return Task.CompletedTask;
        }

        private void InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"SHLauncher - {GetAssemblyVersion()}";
            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;

                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;

                    break;
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            _mainWindowViewModel.IsGlobalLoading = true;
            try
            {
                await _supabaseService.Client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Supabase SignOut: {ex.Message}");
            }
            _sessionService.ClearCurrentUser();
            _authService.ClearRememberedUser();
            _navigationService.Navigate(typeof(Views.Pages.WelcomePage));
            _mainWindowViewModel.IsGlobalLoading = false;
        }

        [RelayCommand]
        private void GoBack()
        {
            var navigationControl = _navigationService.GetNavigationControl();
            if (navigationControl != null && navigationControl.CanGoBack)
            {
                _navigationService.GoBack();
            }
            else // Si no puede ir atrás (probablemente está en MainLauncherPage), ir a MainLauncherPage
            {
                _navigationService.Navigate(typeof(Views.Pages.MainLauncherPage));
            }
        }
    }
}
