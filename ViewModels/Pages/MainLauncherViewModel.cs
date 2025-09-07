using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using shlauncher.Models;

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
        private readonly PipeServerService _pipeServerService;

        [ObservableProperty]
        private bool _isPlayButtonEnabled = true;

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

        public MainLauncherViewModel(
            INavigationService navigationService,
            CurrentUserSessionService sessionService,
            SupabaseService supabaseService,
            MainWindowViewModel mainWindowViewModel,
            AuthService authService,
            PipeServerService pipeServerService)
        {
            _navigationService = navigationService;
            _sessionService = sessionService;
            _supabaseService = supabaseService;
            _mainWindowViewModel = mainWindowViewModel;
            _authService = authService;
            _pipeServerService = pipeServerService;
            _httpClient = new HttpClient();
            PageTitle = "Home - SHLauncher";
        }

        public override async Task OnNavigatedToAsync()
        {
            _mainWindowViewModel.IsGlobalLoading = true;
            LoadUserDataAndLicense();
            if (_sessionService.IsUserLoggedIn)
            {
                await FetchAndUpdateLogs();
                await CheckVersion();
            }
            _mainWindowViewModel.IsGlobalLoading = false;
            await base.OnNavigatedToAsync();
        }

        private void LoadUserDataAndLicense()
        {
            if (_sessionService.IsUserLoggedIn && _sessionService.CurrentProfile != null)
            {
                UserLogin = _sessionService.CurrentProfile.Login;
                if (!string.IsNullOrEmpty(UserLogin))
                {
                    UserAvatarFallback = UserLogin.Length > 0 ? UserLogin[0].ToString().ToUpper() : "U";
                }
                else
                {
                    UserLogin = "User";
                    UserAvatarFallback = "U";
                }
                LicenseType = _sessionService.CurrentProfile.IsBuyer ? "Buyer" : "N/A";
            }
            else
            {
                UserLogin = "Guest";
                UserAvatarFallback = "G";
                LicenseType = "N/A";
                _navigationService.Navigate(typeof(Views.Pages.WelcomePage));
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

        [RelayCommand(CanExecute = nameof(IsPlayButtonEnabled))]
        private async Task Play()
        {
            IsPlayButtonEnabled = false;
            _mainWindowViewModel.IsGlobalLoading = true;

            string? currentPipeToken = _sessionService.PipeToken;

            if (string.IsNullOrEmpty(currentPipeToken))
            {
                Debug.WriteLine("[MainLauncherViewModel.Play] No pipe token (Supabase AccessToken) available.");
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var noTokenMsgBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Authentication Error",
                        Content = "No active session token. Please log in again.",
                        CloseButtonText = "OK"
                    };
                    await noTokenMsgBox.ShowDialogAsync();
                });
                _navigationService.Navigate(typeof(Views.Pages.SignInPage));
                _mainWindowViewModel.IsGlobalLoading = false;
                IsPlayButtonEnabled = true;
                return;
            }

            string pipeName = $"shlauncher-pipe-{Guid.NewGuid()}";
            string mainAppExecutableName = "skinhunter.exe";
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
                        string skinHunterProjectDirName = "skinhunter";
                        string targetFrameworkName = currentDirInfo.Name;
                        string configurationName = currentDirInfo.Parent?.Name ?? "Debug";
                        string devMainAppPath = Path.Combine(solutionDir.FullName, skinHunterProjectDirName, "bin", configurationName, targetFrameworkName, mainAppExecutableName);
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

            if (!File.Exists(mainAppPath))
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var notFoundMsgBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Application Not Found",
                        Content = $"{mainAppExecutableName} not found.\nSearched at: {mainAppPath}\nAnd common development paths. Please ensure it's in the correct directory.",
                        CloseButtonText = "OK"
                    };
                    await notFoundMsgBox.ShowDialogAsync();
                });
                _mainWindowViewModel.IsGlobalLoading = false;
                IsPlayButtonEnabled = true;
                return;
            }

            bool launchAndPipeSuccess = await Task.Run(async () =>
            {
                Process? skinhunterProcess = null;
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(mainAppPath)
                    {
                        Arguments = $"--pipe-name \"{pipeName}\"",
                        UseShellExecute = false
                    };
                    skinhunterProcess = Process.Start(startInfo);

                    if (skinhunterProcess == null)
                    {
                        throw new InvalidOperationException("Failed to start the skinhunter process.");
                    }
                    Debug.WriteLine($"[MainLauncherViewModel.Play.TaskRun] Launched {Path.GetFileName(mainAppPath)} with PID: {skinhunterProcess.Id} and pipe: {pipeName}");

                    bool tokenSentSuccessfully;
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    {
                        try
                        {
                            tokenSentSuccessfully = await _pipeServerService.SendTokenAsync(pipeName, currentPipeToken, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine($"[MainLauncherViewModel.Play.TaskRun] SendTokenAsync was cancelled for pipe {pipeName}.");
                            tokenSentSuccessfully = false;
                            if (skinhunterProcess != null && !skinhunterProcess.HasExited)
                            {
                                try { skinhunterProcess.Kill(true); } catch (Exception killEx) { Debug.WriteLine($"Failed to kill skinhunter process: {killEx.Message}"); }
                            }
                        }
                    }
                    return tokenSentSuccessfully;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainLauncherViewModel.Play.TaskRun] Exception: {ex.Message}");
                    if (skinhunterProcess != null && !skinhunterProcess.HasExited)
                    {
                        try { skinhunterProcess.Kill(true); } catch (Exception killEx) { Debug.WriteLine($"Failed to kill skinhunter process: {killEx.Message}"); }
                    }

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        var launchErrorMsgBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "Launch Error (Background Task)",
                            Content = $"Failed to start or communicate with {Path.GetFileName(mainAppPath)}:\n{ex.Message}",
                            CloseButtonText = "OK"
                        };
                        await launchErrorMsgBox.ShowDialogAsync();
                    });
                    return false;
                }
            });

            _mainWindowViewModel.IsGlobalLoading = false;

            if (launchAndPipeSuccess)
            {
                Debug.WriteLine($"[MainLauncherViewModel.Play] Token exchange reported as successful. Shutting down launcher.");
                Application.Current.Shutdown();
            }
            else
            {
                Debug.WriteLine($"[MainLauncherViewModel.Play] Token exchange or launch FAILED. Launcher will NOT shut down.");
                IsPlayButtonEnabled = true;
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            _mainWindowViewModel.IsGlobalLoading = true;
            try
            {
                await _supabaseService.Client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Supabase SignOut: {ex.Message}");
            }
            _sessionService.ClearCurrentUser();
            _authService.ClearRememberedUser();
            _navigationService.Navigate(typeof(Views.Pages.WelcomePage));
            _mainWindowViewModel.IsGlobalLoading = false;
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            _navigationService.Navigate(typeof(Views.Pages.SettingsPage));
        }
    }
}