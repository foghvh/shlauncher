using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace shlauncher
{
    public partial class App
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => {
                string? basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
                if (!string.IsNullOrEmpty(basePath))
                {
                    c.SetBasePath(basePath);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<INavigationService, NavigationService>();

                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<SupabaseService>();
                services.AddSingleton<AuthService>();
                services.AddSingleton<CurrentUserSessionService>();

                services.AddTransient<WelcomeViewModel>();
                services.AddTransient<SignInViewModel>();
                services.AddTransient<LoadingViewModel>();
                services.AddTransient<MainLauncherViewModel>();
                services.AddSingleton<SettingsViewModel>();

                services.AddTransient<WelcomePage>();
                services.AddTransient<SignInPage>();
                services.AddTransient<LoadingPage>();
                services.AddTransient<MainLauncherPage>();
                services.AddSingleton<SettingsPage>();
            }).Build();

        public static IServiceProvider Services => _host.Services;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();

            var supabaseService = Services.GetRequiredService<SupabaseService>();
            await supabaseService.InitializeAsync();

            var authService = Services.GetRequiredService<AuthService>();
            var sessionService = Services.GetRequiredService<CurrentUserSessionService>();
            var (token, username) = authService.GetRememberedUser();

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
            {
                var principal = authService.ValidateToken(token);
                if (principal?.Identity?.IsAuthenticated == true)
                {
                    Models.User? rememberedUser = await supabaseService.GetUserByLogin(username);
                    if (rememberedUser != null)
                    {
                        sessionService.SetCurrentUser(rememberedUser, token);
                        Debug.WriteLine($"User {rememberedUser.Login} session restored from remembered token.");
                    }
                    else
                    {
                        authService.ClearRememberedUser();
                    }
                }
                else
                {
                    authService.ClearRememberedUser();
                }
            }
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"Unhandled exception: {e.Exception}");
        }
    }
}