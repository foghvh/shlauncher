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
        private readonly CurrentUserSessionService _sessionService; // Para verificar estado de login

        public ApplicationHostService(IServiceProvider serviceProvider, CurrentUserSessionService sessionService)
        {
            _serviceProvider = serviceProvider;
            _sessionService = sessionService;
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
            // La lógica de restauración de sesión ya está en App.OnStartup()
            // Aquí solo mostramos la ventana y navegamos a la página inicial correcta.
            if (!System.Windows.Application.Current.Windows.OfType<INavigationWindow>().Any())
            {
                _navigationWindow = _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow;
                if (_navigationWindow != null)
                {
                    _navigationWindow.ShowWindow();
                    if (_sessionService.IsUserLoggedIn)
                    {
                        _navigationWindow.Navigate(typeof(Views.Pages.MainLauncherPage));
                    }
                    else
                    {
                        _navigationWindow.Navigate(typeof(Views.Pages.WelcomePage));
                    }
                }
            }
            await Task.CompletedTask;
        }
    }
}
