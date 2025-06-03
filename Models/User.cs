using Postgrest.Models;
using Postgrest.Attributes;
using System;

namespace shlauncher.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("password")]
        public string? PasswordHash { get; set; }

        [Column("login")]
        public string? Login { get; set; }

        [Column("fichasporskin")]
        public int Credits { get; set; }

        [Column("escomprador")]
        public bool IsBuyer { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}