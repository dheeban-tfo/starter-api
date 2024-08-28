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
   private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ITenantDbContextAccessor contextAccessor, 
        IEmailService emailService,
        ILogger<PasswordResetService> logger)
    {
        _contextAccessor = contextAccessor;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
       var context = _contextAccessor.TenantDbContext;
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return; // Don't reveal that the user doesn't exist

        var token = GeneratePasswordResetToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

        await context.SaveChangesAsync();

        _emailService.EnqueuePasswordResetEmail(email, token);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var context = _contextAccessor.TenantDbContext;
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.PasswordResetToken == token
        );
        if (user == null || user.PasswordResetTokenExpires < DateTime.UtcNow)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;

        await context.SaveChangesAsync();
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
