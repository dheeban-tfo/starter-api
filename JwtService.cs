using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;


namespace starterapi;

public interface IJwtService
{
    string GenerateToken(User user);
}


public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ApplicationDbContext context, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public string GenerateToken(User user)
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
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}")
        };

        // Add roles and permissions
        var userRoles = _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role)
            .ToList();

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

            var permissions = _context.RoleModulePermissions
                .Where(rmp => rmp.RoleId == role.Id)
                .Select(rmp => new { rmp.Module.Name, rmp.Permission })
                .ToList();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim($"{role.Name}-{permission.Name}", permission.Permission));
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
}