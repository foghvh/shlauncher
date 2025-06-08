using Postgrest.Models;
using Postgrest.Attributes;
using System;
// Quitar: using System.Text.Json.Nodes;
using System.Collections.Generic; // Para Dictionary

namespace shlauncher.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("login")]
        public string? Login { get; set; }

        [Column("is_buyer")]
        public bool IsBuyer { get; set; }

        [Column("avatar_id")]
        public string? AvatarId { get; set; }

        [Column("preferences")]
        public Dictionary<string, object?>? Preferences { get; set; } // Cambiado aquí

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    // Modelo para las preferencias (ejemplo, se puede usar directamente en el diccionario)
    public class UserPreferences
    {
        public string? Theme { get; set; }
        // Otros campos de preferencias aquí
    }
}
