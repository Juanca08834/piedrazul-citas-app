using System.Security.Claims;

namespace Piedrazul.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetSubject(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("sub")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? "anonymous";
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name)
               ?? user.FindFirstValue("preferred_username")
               ?? "Usuario";
    }

    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
               ?? user.FindFirstValue("email");
    }
}
