namespace shlauncher.Services
{
    public partial class CurrentUserSessionService : ObservableObject
    {
        [ObservableProperty]
        private Models.User? _currentUser;

        [ObservableProperty]
        private string? _sessionToken;

        public bool IsUserLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(SessionToken);

        public void SetCurrentUser(Models.User user, string token)
        {
            CurrentUser = user;
            SessionToken = token;
            OnPropertyChanged(nameof(IsUserLoggedIn));
        }

        public void ClearCurrentUser()
        {
            CurrentUser = null;
            SessionToken = null;
            OnPropertyChanged(nameof(IsUserLoggedIn));
        }
    }
}