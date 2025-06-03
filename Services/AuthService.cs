using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCryptNet = BCrypt.Net;

namespace shlauncher.Services
{
    public class AuthService
    {
        private readonly SupabaseService _supabaseService;
        private const string JWTSecret = "skinhunterlaputaquetepariohntvigilacortawachodevuelvanaloan";
        private const string Entropy = "SHLAUNCHER_ENTROPY";

        public AuthService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<(bool Success, string? Token, Models.User? UserData, string? ErrorMessage)> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _supabaseService.GetUserByLogin(username);

                if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                {
                    return (false, null, null, "User not found or password not set.");
                }

                bool isPasswordValid = BCryptNet.BCrypt.Verify(password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    return (false, null, null, "Incorrect password.");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(JWTSecret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("id", user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Login ?? "")
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                string jwtToken = tokenHandler.WriteToken(token);

                return (true, jwtToken, user, null);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Login exception: {ex.Message}");
                return (false, null, null, $"Login failed: {ex.Message}");
            }
        }

        public void RememberUser(string token, string username)
        {
            try
            {
                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                byte[] entropyBytes = Encoding.UTF8.GetBytes(Entropy);
                byte[] protectedToken = ProtectedData.Protect(tokenBytes, entropyBytes, DataProtectionScope.CurrentUser);

                Properties.Settings.Default.RememberedToken = Convert.ToBase64String(protectedToken);
                Properties.Settings.Default.RememberedUsername = username;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to remember user: {ex.Message}");
            }
        }

        public (string? Token, string? Username) GetRememberedUser()
        {
            try
            {
                string? base64ProtectedToken = Properties.Settings.Default.RememberedToken;
                string? rememberedUsername = Properties.Settings.Default.RememberedUsername;

                if (!string.IsNullOrEmpty(base64ProtectedToken) && !string.IsNullOrEmpty(rememberedUsername))
                {
                    byte[] protectedToken = Convert.FromBase64String(base64ProtectedToken);
                    byte[] entropyBytes = Encoding.UTF8.GetBytes(Entropy);
                    byte[] tokenBytes = ProtectedData.Unprotect(protectedToken, entropyBytes, DataProtectionScope.CurrentUser);
                    string token = Encoding.UTF8.GetString(tokenBytes);
                    return (token, rememberedUsername);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get remembered user: {ex.Message}. Clearing remembered data.");
                ClearRememberedUser();
            }
            return (null, null);
        }

        public void ClearRememberedUser()
        {
            Properties.Settings.Default.RememberedToken = string.Empty;
            Properties.Settings.Default.RememberedUsername = string.Empty;
            Properties.Settings.Default.Save();
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(JWTSecret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claimsIdentity = new ClaimsIdentity(jwtToken.Claims);
                return new ClaimsPrincipal(claimsIdentity);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Token validation failed: {ex.Message}");
                return null;
            }
        }
    }
}