using System.Security.Claims;

namespace StarterApi.Helpers
{
public static class UserContext
{
    private static IHttpContextAccessor _httpContextAccessor;
    
    public static void Configure(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public static string CurrentUserId
    {
        get
        {
            var claims = _httpContextAccessor?.HttpContext?.User?.Claims;
            if (claims == null)
            {
                throw new UnauthorizedAccessException("No claims found in token");
            }

            // Try to get user ID from different possible claim types
            var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                     ?? claims.FirstOrDefault(c => c.Type == "nameid")?.Value
                     ?? claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                // Log all available claims for debugging
                var availableClaims = string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}"));
                throw new UnauthorizedAccessException($"User ID not found in token. Available claims: {availableClaims}");
            }

            return userId;
        }
    }

    public static bool TryGetCurrentUserId(out string userId)
    {
        try
        {
            userId = CurrentUserId;
            return true;
        }
        catch
        {
            userId = null;
            return false;
        }
    }

    // Helper method to debug claims
    public static string GetAllClaims()
    {
        var claims = _httpContextAccessor?.HttpContext?.User?.Claims;
        if (claims == null) return "No claims found";
        
        return string.Join("\n", claims.Select(c => $"{c.Type}: {c.Value}"));
    }
}
}