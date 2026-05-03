using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Piedrazul.Infrastructure.Security;

public static class KeycloakClaimsEnricher
{
    public static Task EnrichAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity || context.SecurityToken is not JwtSecurityToken jwtToken)
        {
            return Task.CompletedTask;
        }

        foreach (var role in ExtractRealmRoles(jwtToken))
        {
            if (!identity.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        if (jwtToken.Payload.TryGetValue("preferred_username", out var preferredUsername) && preferredUsername is string username)
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, username));
        }

        if (jwtToken.Payload.TryGetValue("email", out var email) && email is string emailValue)
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, emailValue));
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<string> ExtractRealmRoles(JwtSecurityToken jwtToken)
    {
        if (!jwtToken.Payload.TryGetValue("realm_access", out var realmAccess))
        {
            return Array.Empty<string>();
        }

        if (realmAccess is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("roles", out var rolesElement))
        {
            return rolesElement.EnumerateArray().Select(x => x.GetString()).OfType<string>();
        }

        if (realmAccess is string rawJson)
        {
            using var document = JsonDocument.Parse(rawJson);
            if (document.RootElement.TryGetProperty("roles", out var rolesElementString))
            {
                return rolesElementString.EnumerateArray().Select(x => x.GetString()).OfType<string>().ToArray();
            }
        }

        return Array.Empty<string>();
    }
}
