using System;
using System.Collections.Generic;
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
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public ICollection<UserRole> UserRoles { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }

    // Updated to allow null
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpires { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}