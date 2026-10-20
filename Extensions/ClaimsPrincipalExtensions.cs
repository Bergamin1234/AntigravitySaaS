using System.Security.Claims;

namespace AntigravitySaaS.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? principal.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}
