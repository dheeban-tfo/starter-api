using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using starterapi.Models;
using starterapi.Services;

namespace starterapi;

public interface IJwtService
{
    string GenerateToken(User user, Tenant tenant);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        IConfiguration configuration, 
        ITenantDbContextAccessor contextAccessor, 
        ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public string GenerateToken(User user, Tenant tenant)
    {
        _logger.LogInformation("Generating token for user: {UserId}", user.Id);

        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var jwtExpiryMinutes = _configuration["Jwt:ExpiryInMinutes"];

        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || 
            string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtExpiryMinutes))
        {
            _logger.LogError("JWT configuration is incomplete");
            throw new InvalidOperationException("JWT configuration is incomplete");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("TenantId", tenant.Identifier)
        };

        var tenantDbContext = _contextAccessor.TenantDbContext;

        // Add roles and permissions
        var userRoles = tenantDbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .ToList();

        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));

            var permissions = tenantDbContext.RoleModulePermissions
                .Where(rmp => rmp.RoleId == userRole.RoleId)
                .Include(rmp => rmp.Module)
                .ToList();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim($"{userRole.Role.Name}-{permission.Module.Name}", permission.Permission));
            }
        }

        _logger.LogInformation("Claims added to token:");
        foreach (var claim in claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtExpiryMinutes)),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogInformation("Token generated successfully");

        return tokenString;
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}