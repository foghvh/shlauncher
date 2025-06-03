using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace shlauncher.Models
{
    public class SupabaseUpdateLogEntry
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("changes")]
        public List<string>? Changes { get; set; }
    }
}