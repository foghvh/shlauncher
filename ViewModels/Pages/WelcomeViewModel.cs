using System.Threading.Tasks;
using System.Diagnostics;

namespace shlauncher.ViewModels.Pages
{
    public partial class WelcomeViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;

        public WelcomeViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            PageTitle = "Welcome - SHLauncher";
        }

        [RelayCommand]
        private void NavigateToSignIn()
        {
            _navigationService.Navigate(typeof(Views.Pages.SignInPage));
        }

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
    }
}