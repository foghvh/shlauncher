using System.Threading.Tasks;
using System.Diagnostics;

namespace shlauncher.ViewModels.Pages
{
    public partial class WelcomeViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly CurrentUserSessionService _sessionService;

        public WelcomeViewModel(INavigationService navigationService, CurrentUserSessionService sessionService)
        {
            _navigationService = navigationService;
            _sessionService = sessionService;
            PageTitle = "Welcome - SHLauncher";
        }

        [RelayCommand]
        private void NavigateToSignIn()
        {
            _navigationService.Navigate(typeof(Views.Pages.SignInPage));
        }

        // Este comando podría eliminarse o cambiar a "Create Account"
        [RelayCommand]
        private void NavigateToSignUp()
        {
            _navigationService.Navigate(typeof(Views.Pages.SignUpPage));
        }

        // El botón BUY se mantiene como estaba, abriendo el link
        [RelayCommand]
        private void Buy()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://skinhunterv2.vercel.app") { UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error opening BUY link: {ex.Message}");
            }
        }

        public override async Task OnNavigatedToAsync()
        {
            // Si el usuario ya está logueado (sesión restaurada), ir directamente a MainLauncherPage
            if (_sessionService.IsUserLoggedIn)
            {
                _navigationService.Navigate(typeof(Views.Pages.MainLauncherPage));
            }
            await base.OnNavigatedToAsync();
        }
    }
}
