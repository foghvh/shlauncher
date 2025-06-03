using System.Threading.Tasks;

namespace shlauncher.ViewModels
{
    public abstract partial class LauncherBaseViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        public virtual Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}