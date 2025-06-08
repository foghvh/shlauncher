
using shlauncher.Models; // Para Profile
using Supabase.Gotrue; // Para Session

namespace shlauncher.Services
{
    public partial class CurrentUserSessionService : ObservableObject
    {
        [ObservableProperty]
        private Models.Profile? _currentProfile;

        [ObservableProperty]
        private Session? _currentSupabaseSession; // Almacenar la sesión de Supabase

        public bool IsUserLoggedIn => CurrentProfile != null && CurrentSupabaseSession?.User != null && !string.IsNullOrEmpty(CurrentSupabaseSession.AccessToken);

        // El AccessToken para el pipe
        public string? PipeToken => CurrentSupabaseSession?.AccessToken;

        public void SetCurrentUser(Models.Profile profile, Session supabaseSession)
        {
            CurrentProfile = profile;
            CurrentSupabaseSession = supabaseSession;
            OnPropertyChanged(nameof(IsUserLoggedIn));
            OnPropertyChanged(nameof(PipeToken));
        }

        public void ClearCurrentUser()
        {
            CurrentProfile = null;
            CurrentSupabaseSession = null;
            // También podrías llamar a _supabaseService.Client.Auth.SignOut() aquí si quieres invalidar la sesión en Supabase.
            OnPropertyChanged(nameof(IsUserLoggedIn));
            OnPropertyChanged(nameof(PipeToken));
        }
    }
}
