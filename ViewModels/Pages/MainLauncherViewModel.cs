using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace shlauncher.ViewModels.Pages
{
    public partial class MainLauncherViewModel : LauncherBaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly CurrentUserSessionService _sessionService;
        private readonly SupabaseService _supabaseService;
        private readonly HttpClient _httpClient;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly AuthService _authService;

        [ObservableProperty]
        private string? _userLogin;

        [ObservableProperty]
        private string? _userAvatarFallback;

        [ObservableProperty]
        private string _patchVersion = "Unknown";

        [ObservableProperty]
        private string _versionStatus = "Checking...";

        [ObservableProperty]
        private string _licenseType = "N/A";

        public ObservableCollection<Models.SupabaseUpdateLogEntry> UpdateLogs { get; } = new();

        public MainLauncherViewModel(INavigationService navigationService, CurrentUserSessionService sessionService, SupabaseService supabaseService, MainWindowViewModel mainWindowViewModel, AuthService authService)
        {
            _navigationService = navigationService;
            _sessionService = sessionService;
            _supabaseService = supabaseService;
            _mainWindowViewModel = mainWindowViewModel;
            _authService = authService;
            _httpClient = new HttpClient();
            PageTitle = "Home - SHLauncher";
        }

        public override async Task OnNavigatedToAsync()
        {
            _mainWindowViewModel.IsGlobalLoading = true;
            LoadUserDataAndLicense();
            await FetchAndUpdateLogs();
            await CheckVersion();
            _mainWindowViewModel.IsGlobalLoading = false;
            await base.OnNavigatedToAsync();
        }

        private void LoadUserDataAndLicense()
        {
            if (_sessionService.IsUserLoggedIn && _sessionService.CurrentUser != null)
            {
                UserLogin = _sessionService.CurrentUser.Login;
                if (!string.IsNullOrEmpty(UserLogin))
                {
                    UserAvatarFallback = UserLogin.Length > 0 ? UserLogin[0].ToString().ToUpper() : "U";
                }
                else
                {
                    UserLogin = "User";
                    UserAvatarFallback = "U";
                }
                LicenseType = _sessionService.CurrentUser.IsBuyer ? "Buyer" : "N/A";
            }
            else
            {
                UserLogin = "Guest";
                UserAvatarFallback = "G";
                LicenseType = "N/A";
                _navigationService.Navigate(typeof(Views.Pages.SignInPage));
            }
        }

        private async Task FetchAndUpdateLogs()
        {
            var logs = await _supabaseService.GetUpdateLogsAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateLogs.Clear();
                if (logs != null)
                {
                    foreach (var logEntry in logs)
                    {
                        UpdateLogs.Add(logEntry);
                    }
                }
                else
                {
                    UpdateLogs.Add(new Models.SupabaseUpdateLogEntry { Title = "INFO", Changes = new List<string> { "Could not load update logs." } });
                }
            });
        }

        private async Task CheckVersion()
        {
            VersionStatus = "Checking...";
            try
            {
                string cdragonVersionString = "";
                try
                {
                    var responseCdragon = await _httpClient.GetAsync("https://raw.communitydragon.org/latest/content-metadata.json");
                    responseCdragon.EnsureSuccessStatusCode();
                    string jsonCdragon = await responseCdragon.Content.ReadAsStringAsync();
                    using var docCdragon = JsonDocument.Parse(jsonCdragon);
                    if (docCdragon.RootElement.TryGetProperty("version", out JsonElement versionElement))
                    {
                        var fullCdragonVersion = versionElement.GetString();
                        var versionParts = fullCdragonVersion?.Split('.').Take(2);
                        if (versionParts != null)
                        {
                            cdragonVersionString = string.Join(".", versionParts);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching CDRAGON version: {ex.Message}");
                    PatchVersion = "DB Error";
                    VersionStatus = "Error";
                    return;
                }

                if (string.IsNullOrEmpty(cdragonVersionString))
                {
                    PatchVersion = "N/A";
                    VersionStatus = "Unknown";
                    return;
                }

                PatchVersion = cdragonVersionString;

                string supabasePatchVersionString = "";
                try
                {
                    byte[]? fileBytes = await _supabaseService.DownloadFileBytesAsync("version", "patch.json");

                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        VersionStatus = "Local N/A";
                        return;
                    }
                    string jsonSupabase = System.Text.Encoding.UTF8.GetString(fileBytes);

                    using var docSupabase = JsonDocument.Parse(jsonSupabase);
                    if (docSupabase.RootElement.TryGetProperty("version", out JsonElement supabaseVersionElement))
                    {
                        var fullSupabaseVersion = supabaseVersionElement.GetString();
                        var versionParts = fullSupabaseVersion?.Split('.').Take(2);
                        if (versionParts != null)
                        {
                            supabasePatchVersionString = string.Join(".", versionParts);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching/parsing Supabase patch.json: {ex.Message}");
                    VersionStatus = "Local Error";
                    return;
                }

                if (string.IsNullOrEmpty(supabasePatchVersionString))
                {
                    VersionStatus = "Local N/A";
                    return;
                }

                VersionStatus = cdragonVersionString.Equals(supabasePatchVersionString, StringComparison.OrdinalIgnoreCase) ? "UPDATED" : "OUTDATED";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking version: {ex.Message}");
                PatchVersion = "Error";
                VersionStatus = "Error";
            }
        }

        [RelayCommand]
        private async Task Play()
        {
            string mainAppExecutableName = "SkinHunterWPF.exe";
            string launcherDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string mainAppPath = Path.Combine(launcherDirectory, mainAppExecutableName);

            if (!File.Exists(mainAppPath))
            {
                try
                {
                    DirectoryInfo? currentDirInfo = new DirectoryInfo(launcherDirectory);
                    DirectoryInfo? binDir = currentDirInfo.Parent?.Parent;
                    DirectoryInfo? projectLauncherDir = binDir?.Parent;
                    DirectoryInfo? solutionDir = projectLauncherDir?.Parent;

                    if (solutionDir != null)
                    {
                        string skinHunterWPFProjectDirName = "SkinHunterWPF";
                        string targetFramework = currentDirInfo.Name;
                        string configuration = currentDirInfo.Parent?.Name ?? "Debug";

                        string devMainAppPath = Path.Combine(solutionDir.FullName, skinHunterWPFProjectDirName, "bin", configuration, targetFramework, mainAppExecutableName);
                        if (File.Exists(devMainAppPath))
                        {
                            mainAppPath = devMainAppPath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during development path discovery: {ex.Message}");
                }
            }

            if (File.Exists(mainAppPath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(mainAppPath)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    var msgBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Launch Error",
                        Content = $"Failed to start main application:\n{ex.Message}",
                        CloseButtonText = "OK"
                    };
                    await msgBox.ShowDialogAsync();
                }
            }
            else
            {
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Application Not Found",
                    Content = $"{mainAppExecutableName} not found.\nSearched at: {mainAppPath}\n(And common development paths)\nPlease ensure it is in the same directory as the launcher or build output paths are correctly configured.",
                    CloseButtonText = "OK"
                };
                await msgBox.ShowDialogAsync();
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
        private void NavigateToSettings()
        {
            _navigationService.Navigate(typeof(Views.Pages.SettingsPage));
        }
    }
}