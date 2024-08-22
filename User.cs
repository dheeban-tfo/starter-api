using System.Text.Json.Serialization;
using starterapi.Repositories;

namespace starterapi;

public class User
{
    public int Id { get; set; }

    [Searchable]
    public string FirstName { get; set; }

    [Searchable]
    public string LastName { get; set; }

    [Searchable]
    public string Email { get; set; }
    public string PasswordHash { get; set; } // Store hashed passwords
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public ICollection<UserRole> UserRoles { get; set; }

     // New fields for password reset
    public string PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
}

