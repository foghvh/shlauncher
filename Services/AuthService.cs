using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Supabase.Gotrue;
using shlauncher.Models;
using System.Collections.Generic;
using Postgrest.Responses;

namespace shlauncher.Services
{
    public class AuthService
    {
        private readonly SupabaseService _supabaseService;
        private const string Entropy = "SHLAUNCHER_ENTROPY_V2_MORE_SECURE";

        public AuthService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<(bool Success, Session? Session, Models.Profile? ProfileData, string? ErrorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                var session = await _supabaseService.Client.Auth.SignIn(email, password);

                if (session?.User?.Id == null || string.IsNullOrEmpty(session.AccessToken))
                {
                    if (session?.User != null && session.User.ConfirmedAt == null)
                    {
                        return (false, null, null, "Login failed. Please confirm your email address first.");
                    }
                    return (false, null, null, "Login failed. Invalid credentials or user not found.");
                }

                string accessTokenShort = session.AccessToken?.Substring(0, Math.Min(session.AccessToken.Length, 10)) ?? "N/A";
                Debug.WriteLine($"Login successful for user {session.User.Id}. Supabase Client CurrentUser: {_supabaseService.Client.Auth.CurrentUser?.Id}, Session AccessToken (short): {accessTokenShort}...");

                Models.Profile? profileData = await _supabaseService.GetUserProfile(Guid.Parse(session.User.Id));
                if (profileData == null && session.User != null)
                {
                    Debug.WriteLine($"Login successful for user {session.User.Id}, but profile data is missing. Attempting to create profile as fallback.");
                    profileData = new Models.Profile
                    {
                        Id = Guid.Parse(session.User.Id),
                        Login = session.User.Email?.Split('@')[0],
                        Preferences = new Dictionary<string, object?> { ["theme"] = "dark" }
                    };
                    var createdProfile = await _supabaseService.InsertNewUserProfile(profileData); // Esto usa la sesión del usuario logueado
                    if (createdProfile == null) Debug.WriteLine($"Failed to create missing profile for user {session.User.Id} after login via fallback.");
                    else profileData = createdProfile;
                }
                else if (profileData != null && string.IsNullOrEmpty(profileData.Login) && session.User != null && !string.IsNullOrEmpty(session.User.Email))
                {
                    Debug.WriteLine($"User {session.User.Id} logged in, profile exists but login is NULL/empty. Setting login from email part as fallback.");
                    profileData.Login = session.User.Email.Split('@')[0];
                    var updatedProfile = await _supabaseService.UpdateExistingUserProfile(profileData);
                    if (updatedProfile != null) profileData = updatedProfile;
                    else Debug.WriteLine($"Failed to update profile login for user {session.User.Id} after login via fallback.");
                }

                return (true, session, profileData, null);
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                Debug.WriteLine($"Supabase Login exception: {ex.Message} (Reason: {ex.Reason})");
                string friendlyMessage = "Login failed. ";
                if (ex.Message.Contains("Invalid login credentials"))
                {
                    friendlyMessage += "Invalid email or password.";
                }
                else if (ex.Message.Contains("Email not confirmed") || ex.Message.Contains("awaiting verification"))
                {
                    friendlyMessage += "Please confirm your email address first.";
                }
                else
                {
                    friendlyMessage += "An error occurred with the authentication service.";
                }
                return (false, null, null, friendlyMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic Login exception: {ex.Message}");
                return (false, null, null, $"Login failed due to an unexpected error: {ex.Message}");
            }
        }

        public async Task<(bool Success, User? User, Models.Profile? ProfileData, string? ErrorMessage)> RegisterAsync(string email, string password, string login)
        {
            try
            {
                var existingProfileByLoginResponse = await _supabaseService.Client.From<Models.Profile>()
                    .Filter("login", Postgrest.Constants.Operator.Equals, login)
                    .Get();
                if (existingProfileByLoginResponse.Models != null && existingProfileByLoginResponse.Models.Any())
                {
                    return (false, null, null, "This login is already taken. Please choose another one.");
                }

                Debug.WriteLine($"Attempting Auth.SignUp with Email: {email}, Login to be passed in metadata: {login}");
                Session? session = null;
                User? signedUpUser = null;

                var signUpOptions = new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "login", login }
                    }
                };

                try
                {
                    session = await _supabaseService.Client.Auth.SignUp(email, password, signUpOptions);
                    signedUpUser = session?.User;
                }
                catch (Supabase.Gotrue.Exceptions.GotrueException gotrueEx)
                {
                    Debug.WriteLine($"Auth.SignUp EXCEPTION: {gotrueEx.Message} (Reason: {gotrueEx.Reason})");
                    throw;
                }

                Debug.WriteLine($"Auth.SignUp call completed.");
                if (signedUpUser == null || string.IsNullOrEmpty(signedUpUser.Id))
                {
                    Debug.WriteLine("Auth.SignUp did not return a valid User object with an ID.");
                    return (false, null, null, "Registration failed: Could not retrieve user details after sign-up attempt.");
                }
                string sessionAccessTokenShort = session?.AccessToken?.Substring(0, Math.Min(session.AccessToken?.Length ?? 0, 10)) ?? "N/A";
                Debug.WriteLine($"User object from SignUp: ID: {signedUpUser.Id}, Email: {signedUpUser.Email}, ConfirmedAt: {signedUpUser.ConfirmedAt}, Session AccessToken Present: {!string.IsNullOrEmpty(session?.AccessToken)} ({sessionAccessTokenShort})");

                // Si se requiere confirmación de email, el AccessToken estará vacío/nulo aquí.
                // Confiamos en que el trigger haya creado el perfil y establecido el login desde metadata.
                // No intentaremos actualizar el perfil desde C# en este punto del flujo de SignUp
                // si se requiere confirmación, ya que no tendríamos un token de sesión válido para hacerlo.
                // El login se verificará/establecerá en el primer SignIn.

                // Obtener el perfil para devolverlo (puede ser null si el trigger falló catastróficamente)
                Models.Profile? createdProfile = await _supabaseService.GetUserProfile(Guid.Parse(signedUpUser.Id));

                if (createdProfile == null)
                {
                    Debug.WriteLine($"WARNING: Profile for user {signedUpUser.Id} was NOT found after SignUp. Trigger failed critically. The user exists in auth.users but not in public.profiles.");
                    // Esto es un estado problemático. El usuario existe en auth pero no tiene perfil.
                    return (true, signedUpUser, null, "Registration partially succeeded. Profile creation pending. Please try logging in after email confirmation, or contact support if issues persist.");
                }

                if (createdProfile.Login != login)
                {
                    Debug.WriteLine($"WARNING: Profile login is '{createdProfile.Login}', but expected '{login}' from metadata. This might happen if the login from metadata was a duplicate and the trigger inserted NULL instead, or if trigger logic for metadata is off.");
                    // En este punto, el usuario está creado, el perfil base existe.
                    // El usuario deberá confirmar email y luego el login se podrá corregir/establecer en el primer SignIn.
                }
                else
                {
                    Debug.WriteLine($"Profile for user {signedUpUser.Id} found/created by trigger with login: '{createdProfile.Login}'.");
                }

                string successMessage = "Registration successful! Please check your email to confirm your account.";
                return (true, signedUpUser, createdProfile, successMessage);
            }
            catch (Supabase.Gotrue.Exceptions.GotrueException ex)
            {
                Debug.WriteLine($"Supabase Registration exception (outer catch): {ex.Message} (Reason: {ex.Reason})");
                string friendlyMessage = "Registration failed. ";
                if (ex.Message.Contains("User already registered"))
                {
                    friendlyMessage += "This email is already registered.";
                }
                else if (ex.Message.Contains("Password should be at least 6 characters"))
                {
                    friendlyMessage += "Password should be at least 6 characters.";
                }
                else if (ex.Message.Contains("Database error saving new user") || ex.Message.Contains("unexpected_failure"))
                {
                    friendlyMessage += "A database error occurred while creating your account. Please try again later or contact support.";
                }
                else
                {
                    friendlyMessage += "An error occurred with the authentication service: " + ex.Reason;
                }
                return (false, null, null, friendlyMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic Registration exception: {ex.Message}. StackTrace: {ex.StackTrace}");
                return (false, null, null, $"Registration failed due to an unexpected error: {ex.Message}");
            }
        }

        public void RememberUser(string accessToken, string? refreshToken, string userId, string userLogin)
        {
            try
            {
                Properties.Settings.Default.RememberedToken = Protect(accessToken);
                Properties.Settings.Default.RememberedRefreshToken = Protect(refreshToken ?? string.Empty);
                Properties.Settings.Default.RememberedUserId = userId;
                Properties.Settings.Default.RememberedUsername = userLogin;
                Properties.Settings.Default.Save();
                Debug.WriteLine($"User {userLogin} ({userId}) tokens remembered.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to remember user: {ex.Message}");
            }
        }

        public async Task<(Session? Session, Models.Profile? ProfileData)> GetRememberedUserSessionAsync()
        {
            try
            {
                string? protectedAccessToken = Properties.Settings.Default.RememberedToken;
                string? protectedRefreshToken = Properties.Settings.Default.RememberedRefreshToken;
                string? userIdString = Properties.Settings.Default.RememberedUserId;
                string? userLogin = Properties.Settings.Default.RememberedUsername;

                if (!string.IsNullOrEmpty(protectedAccessToken) &&
                    protectedRefreshToken != null &&
                    !string.IsNullOrEmpty(userIdString) &&
                    !string.IsNullOrEmpty(userLogin))
                {
                    string accessToken = Unprotect(protectedAccessToken);
                    string refreshTokenString = Unprotect(protectedRefreshToken); // Ya no es string.Empty si está protegido

                    var session = await _supabaseService.Client.Auth.SetSession(accessToken, refreshTokenString, false);

                    if (session?.User?.Id != null && !string.IsNullOrEmpty(session.AccessToken) && Guid.TryParse(userIdString, out Guid userIdGuid))
                    {
                        Debug.WriteLine($"Session potentially restored/refreshed for user ID {session.User.Id} via remembered tokens.");
                        if (session.AccessToken != accessToken || (session.RefreshToken != null && session.RefreshToken != refreshTokenString && !string.IsNullOrEmpty(session.RefreshToken)))
                        {
                            RememberUser(session.AccessToken, session.RefreshToken, session.User.Id, userLogin);
                            Debug.WriteLine("Refreshed tokens saved.");
                        }
                        Models.Profile? profileData = await _supabaseService.GetUserProfile(userIdGuid);
                        return (session, profileData);
                    }
                    else
                    {
                        Debug.WriteLine("Failed to restore session from remembered tokens (SetSession returned null or invalid session/userId). Clearing remembered data.");
                        ClearRememberedUser();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get remembered user session: {ex.Message}. Clearing remembered data.");
                ClearRememberedUser();
            }
            return (null, null);
        }

        public ClaimsPrincipal? ValidateSupabaseAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return null;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(accessToken))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                    var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "SupabaseJWT");
                    return new ClaimsPrincipal(claimsIdentity);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading Supabase access token (not validating signature): {ex.Message}");
                return null;
            }
        }

        public void ClearRememberedUser()
        {
            Properties.Settings.Default.RememberedToken = string.Empty;
            Properties.Settings.Default.RememberedRefreshToken = string.Empty;
            Properties.Settings.Default.RememberedUserId = string.Empty;
            Properties.Settings.Default.RememberedUsername = string.Empty;
            Properties.Settings.Default.Save();
            Debug.WriteLine("Cleared remembered user data.");
        }

        private string Protect(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] entropyBytes = Encoding.UTF8.GetBytes(Entropy);
            byte[] protectedData = ProtectedData.Protect(dataBytes, entropyBytes, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedData);
        }

        private string Unprotect(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData)) return string.Empty;
            byte[] protectedDataBytes = Convert.FromBase64String(protectedData);
            byte[] entropyBytes = Encoding.UTF8.GetBytes(Entropy);
            byte[] dataBytes = ProtectedData.Unprotect(protectedDataBytes, entropyBytes, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dataBytes);
        }
    }
}
