using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace starterapi.Filters;

public class PermissionAuthorizationFilter : IAuthorizationFilter
{
    private readonly IConfiguration _configuration;

    public PermissionAuthorizationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isMandatoryCheck = _configuration.GetValue<bool>("Security:MandatoryPermissionCheck");

        if (!isMandatoryCheck)
        {
            return;
        }

        var hasPermissionAttribute = context.ActionDescriptor.EndpointMetadata.OfType<PermissionAttribute>().Any();
        var hasAuthorizeAttribute = context.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any(a => a.Policy == "PermissionPolicy");

        if (!hasPermissionAttribute || !hasAuthorizeAttribute)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}