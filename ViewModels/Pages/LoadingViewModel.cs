using System.Threading.Tasks;

namespace shlauncher.ViewModels.Pages
{
    public partial class LoadingViewModel : LauncherBaseViewModel // INavigationAware is inherited
    {
        private readonly INavigationService _navigationService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public LoadingViewModel(INavigationService navigationService, MainWindowViewModel mainWindowViewModel)
        {
            _navigationService = navigationService;
            _mainWindowViewModel = mainWindowViewModel;
            PageTitle = "Loading - SHLauncher";
        }

        public override async Task OnNavigatedToAsync() // Changed from (object? parameter = null)
        {
            _mainWindowViewModel.IsGlobalLoading = true;
            await Task.Delay(1500);
            _navigationService.Navigate(typeof(Views.Pages.MainLauncherPage));
            _mainWindowViewModel.IsGlobalLoading = false;
            await base.OnNavigatedToAsync();
        }
    }
}