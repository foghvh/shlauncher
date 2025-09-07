using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Supabase.Gotrue; // Para Session

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
                services.AddTransient<PipeServerService>();
                services.AddTransient<WelcomeViewModel>();
                services.AddTransient<SignInViewModel>();
                services.AddTransient<SignUpViewModel>(); // Añadir SignUpViewModel
                services.AddTransient<LoadingViewModel>();
                services.AddTransient<MainLauncherViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddTransient<WelcomePage>();
                services.AddTransient<SignInPage>();
                services.AddTransient<SignUpPage>(); // Añadir SignUpPage
                services.AddTransient<LoadingPage>();
                services.AddTransient<MainLauncherPage>();
                services.AddSingleton<SettingsPage>();
            }).Build();

        public static IServiceProvider Services => _host.Services;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();

            var supabaseService = Services.GetRequiredService<SupabaseService>();
            await supabaseService.InitializeAsync(); // Asegura que el cliente Supabase esté listo

            // Obtener AuthService para activar la suscripción a eventos de refresco.
            var authService = Services.GetRequiredService<AuthService>();
            var sessionService = Services.GetRequiredService<CurrentUserSessionService>();

            // Intenta restaurar la sesión del usuario recordado
            var (rememberedSession, rememberedProfile) = await authService.GetRememberedUserSessionAsync();

            if (rememberedSession?.User?.Id != null && rememberedProfile != null)
            {
                sessionService.SetCurrentUser(rememberedProfile, rememberedSession);
                Debug.WriteLine($"User {rememberedProfile.Login} session restored from remembered data.");
                // El ApplicationHostService navegará a MainLauncherPage si está logueado
            }
            else
            {
                Debug.WriteLine("No valid remembered user session found or profile missing.");
                authService.ClearRememberedUser(); // Limpia si los datos no son válidos o están incompletos
                // El ApplicationHostService navegará a WelcomePage
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
            // Podrías mostrar un MessageBox aquí si lo deseas
            // e.Handled = true; // Descomenta si quieres evitar que la aplicación se cierre
        }
    }
}
