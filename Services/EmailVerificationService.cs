using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace starterapi.Services;

public interface IEmailVerificationService
{
    Task<string> GenerateVerificationTokenAsync(User user);
    Task<bool> VerifyEmailAsync(int userId, string token);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public EmailVerificationService(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<string> GenerateVerificationTokenAsync(User user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours
        await _context.SaveChangesAsync();

        var verificationLink = $"{_configuration["AppUrl"]}/verify-email?userId={user.Id}&token={token}";
        var emailBody = $"Please verify your email by clicking on this link: {verificationLink}";

        // Send verification email using Mailjet
        await _emailService.SendEmailAsync(user.Email, "Verify your email", emailBody);

        return token;
    }

    public async Task<bool> VerifyEmailAsync(int userId, string token)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.EmailVerificationToken != token || user.EmailVerificationTokenExpires < DateTime.UtcNow)
        {
            return false;
        }

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;
        await _context.SaveChangesAsync();

        return true;
    }
}