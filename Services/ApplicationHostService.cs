using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace shlauncher.Services
{
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private INavigationWindow? _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task HandleActivationAsync()
        {
            if (!System.Windows.Application.Current.Windows.OfType<INavigationWindow>().Any())
            {
                _navigationWindow = _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow;
                _navigationWindow!.ShowWindow();
                _navigationWindow.Navigate(typeof(Views.Pages.WelcomePage));
            }
            await Task.CompletedTask;
        }
    }
}