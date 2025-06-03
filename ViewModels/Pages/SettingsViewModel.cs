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

        public SettingsViewModel(CurrentUserSessionService sessionService, INavigationService navigationService, AuthService authService)
        {
            _sessionService = sessionService;
            _navigationService = navigationService;
            _authService = authService;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            _isInitialized = false;
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
        private void Logout()
        {
            _sessionService.ClearCurrentUser();
            _authService.ClearRememberedUser();
            _navigationService.Navigate(typeof(Views.Pages.WelcomePage));
        }

        [RelayCommand]
        private void GoBack()
        {
            var navigationControl = _navigationService.GetNavigationControl();
            if (navigationControl != null && navigationControl.CanGoBack)
            {
                _navigationService.GoBack();
            }
        }
    }
}