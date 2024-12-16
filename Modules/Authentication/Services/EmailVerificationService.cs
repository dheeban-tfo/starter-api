using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace starterapi.Services;

public interface IEmailVerificationService
{
    Task<string> GenerateVerificationTokenAsync(User user);
    Task<bool> VerifyEmailAsync(Guid userId, string token);
}

public class EmailVerificationService : IEmailVerificationService
{
 private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        ITenantDbContextAccessor contextAccessor, 
        IEmailService emailService, 
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _contextAccessor = contextAccessor;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateVerificationTokenAsync(User user)
    {
        var context = _contextAccessor.TenantDbContext;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);
        await context.SaveChangesAsync();

        var verificationLink = $"{_configuration["AppUrl"]}/verify-email?userId={user.Id}&token={token}";
        var emailBody = $"Please verify your email by clicking on this link: {verificationLink}";

        // Send verification email using Mailjet
        await _emailService.SendEmailAsync(user.Email, "Verify your email", emailBody);

        return token;
    }

    public async Task<bool> VerifyEmailAsync(Guid userId, string token)
    {
       var context = _contextAccessor.TenantDbContext;
        var user = await context.Users.FindAsync(userId);
        if (user == null || user.EmailVerificationToken != token || user.EmailVerificationTokenExpires < DateTime.UtcNow)
        {
            return false;
        }

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;
        await context.SaveChangesAsync();

        return true;
    }
}