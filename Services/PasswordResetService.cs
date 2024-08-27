using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace starterapi.Services;

public interface IPasswordResetService
{
    Task RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly TenantDbContext _context;
    private readonly IEmailService _emailService;

    public PasswordResetService(TenantDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return; // Don't reveal that the user doesn't exist

        var token = GeneratePasswordResetToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

        await _context.SaveChangesAsync();

        _emailService.EnqueuePasswordResetEmail(email, token);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.PasswordResetToken == token
        );
        if (user == null || user.PasswordResetTokenExpires < DateTime.UtcNow)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;

        await _context.SaveChangesAsync();
        return true;
    }

    private string GeneratePasswordResetToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
