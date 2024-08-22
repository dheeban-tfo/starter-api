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

    public JwtService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string GenerateToken(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured."));

        var userRoles = _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role)
            .ToList();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

            var rolePermissions = _context.RoleModulePermissions
                .Include(rp => rp.Module)
                .Where(rp => rp.RoleId == role.Id)
                .ToList();

            foreach (var permission in rolePermissions)
            {
                claims.Add(new Claim($"{role.Name}-{permission.Module.Name}", permission.Permission));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"] ?? "60")),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}