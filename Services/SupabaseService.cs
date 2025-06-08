using Supabase;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Client = Supabase.Client;
using Supabase.Gotrue;
using System.Text.Json;
using System.Net.Http;
using System;
using System.Diagnostics;
using shlauncher.Models;
using Postgrest.Responses;
using Postgrest.Models;
using Postgrest;
using System.Net;

namespace shlauncher.Services
{
    public class SupabaseService
    {
        private readonly Client _supabase;
        private readonly HttpClient _httpClient;

        private const string SupabaseUrl = "https://odlqwkgewzxxmbsqutja.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9kbHF3a2dld3p4eG1ic3F1dGphIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzQyMTM2NzcsImV4cCI6MjA0OTc4OTY3N30.qka6a71bavDeUQgy_BKoVavaClRQa_gT36Au7oO9AF0";

        public Client Client => _supabase;

        public SupabaseService()
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
            };
            _supabase = new Client(SupabaseUrl, SupabaseAnonKey, options);
            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            await _supabase.InitializeAsync();
            Debug.WriteLine($"Supabase initialized. Current User: {_supabase.Auth.CurrentUser?.Email}");
        }

        public async Task<Models.Profile?> GetUserProfile(Guid userId)
        {
            try
            {
                var response = await _supabase.From<Models.Profile>()
                                          .Filter("id", Postgrest.Constants.Operator.Equals, userId.ToString())
                                          .Single();
                return response;
            }
            catch (Postgrest.Exceptions.PostgrestException pgEx) when (pgEx.Message.Contains("JSON object requested, multiple (or no) rows returned") || pgEx.Content?.Contains("PGRST116") == true)
            {
                Debug.WriteLine($"GetUserProfile for {userId}: No profile found. Message: {pgEx.Message}, StatusCode: {pgEx.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching user profile for {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task<Models.Profile?> InsertNewUserProfile(Models.Profile profileData)
        {
            try
            {
                ModeledResponse<Models.Profile> response = await _supabase.From<Models.Profile>()
                                                                   .Insert(profileData);

                if (response.ResponseMessage != null && response.ResponseMessage.IsSuccessStatusCode)
                {
                    if (response.Models != null && response.Models.Any())
                    {
                        Debug.WriteLine($"Profile inserted successfully (model returned) for ID: {response.Models.First().Id}, Login: {response.Models.First().Login}");
                        return response.Models.First();
                    }
                    // Si la inserción fue exitosa pero no devolvió el modelo, lo volvemos a obtener.
                    Debug.WriteLine($"Profile insert HTTP call successful for ID: {profileData.Id}, but no model returned in response. Fetching again.");
                    return await GetUserProfile(profileData.Id);
                }
                Debug.WriteLine($"Failed to insert new profile. HttpStatusCode: {response.ResponseMessage?.StatusCode}, Content: {response.Content}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting new profile for {profileData.Id}: {ex.Message}");
                if (ex is Postgrest.Exceptions.PostgrestException pgEx) { Debug.WriteLine($"PostgrestDetails: {pgEx.Content}, StatusCode: {pgEx.StatusCode}"); }
                return null;
            }
        }

        public async Task<Models.Profile?> UpdateExistingUserProfile(Models.Profile profileData)
        {
            try
            {
                ModeledResponse<Models.Profile> response = await _supabase.From<Models.Profile>()
                                                                   .Where(x => x.Id == profileData.Id)
                                                                   .Update(profileData);

                if (response.ResponseMessage != null && response.ResponseMessage.IsSuccessStatusCode)
                {
                    if (response.Models != null && response.Models.Any())
                    {
                        Debug.WriteLine($"Profile updated successfully (model returned) for ID: {response.Models.First().Id}, New Login: {response.Models.First().Login}");
                        return response.Models.First();
                    }
                    Debug.WriteLine($"Update HTTP call was success for ID: {profileData.Id}, but no model returned in response. Fetching profile again.");
                    return await GetUserProfile(profileData.Id);
                }
                Debug.WriteLine($"Failed to update profile. HttpStatusCode: {response.ResponseMessage?.StatusCode}, Content: {response.Content}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating profile for {profileData.Id}: {ex.Message}");
                if (ex is Postgrest.Exceptions.PostgrestException pgEx) { Debug.WriteLine($"PostgrestDetails: {pgEx.Content}, StatusCode: {pgEx.StatusCode}"); }
                return null;
            }
        }

        public async Task<byte[]?> DownloadFileBytesAsync(string bucketName, string filePathInBucket)
        {
            try
            {
                string storageUrlPart = "/storage/v1";
                string baseUrl = SupabaseUrl.EndsWith(storageUrlPart) ? SupabaseUrl : SupabaseUrl + storageUrlPart;
                string publicUrl = $"{baseUrl}/object/public/{bucketName}/{filePathInBucket}";
                publicUrl = publicUrl.Replace("//object", "/object");

                HttpResponseMessage response = await _httpClient.GetAsync(publicUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error downloading file from Supabase Storage. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent} (URL: {publicUrl})");
                    return null;
                }
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HttpRequestException downloading file from Supabase Storage: {ex.Message} (URL: {bucketName}/{filePathInBucket})");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic error downloading file {bucketName}/{filePathInBucket}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Models.SupabaseUpdateLogEntry>?> GetUpdateLogsAsync()
        {
            string bucketName = "version";
            string filePathInBucket = "updates.json";
            try
            {
                byte[]? fileBytes = await DownloadFileBytesAsync(bucketName, filePathInBucket);
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    Debug.WriteLine($"Supabase {filePathInBucket} not found or is empty in bucket '{bucketName}'.");
                    return null;
                }
                string jsonContent = System.Text.Encoding.UTF8.GetString(fileBytes);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Models.SupabaseUpdateLogEntry>>(jsonContent, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching/parsing Supabase {filePathInBucket}: {ex.Message}");
                return null;
            }
        }
    }
}
