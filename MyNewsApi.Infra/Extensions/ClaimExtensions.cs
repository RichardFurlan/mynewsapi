using System.Security.Claims;

namespace MyNewsApi.Infra.Extensions;

public static class ClaimExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id))
            return id;
        return null;
    }
}
