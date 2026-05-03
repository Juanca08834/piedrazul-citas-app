using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Piedrazul.Infrastructure.Security;

public sealed class DevelopmentAuthOptions
{
    public string DefaultSubject { get; set; } = "demo-patient";
    public string DefaultName { get; set; } = "Paciente Demo";
    public string? DefaultEmail { get; set; } = "paciente.demo@piedrazul.test";
    public string[] DefaultRoles { get; set; } = ["Patient"];
}

public sealed class DevelopmentAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    IOptions<DevelopmentAuthOptions> developmentAuthOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    private readonly DevelopmentAuthOptions _developmentAuthOptions = developmentAuthOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers["X-Debug-Subject"].FirstOrDefault() ?? _developmentAuthOptions.DefaultSubject;
        var name = Request.Headers["X-Debug-Name"].FirstOrDefault() ?? _developmentAuthOptions.DefaultName;
        var email = Request.Headers["X-Debug-Email"].FirstOrDefault() ?? _developmentAuthOptions.DefaultEmail;
        var rolesHeader = Request.Headers["X-Debug-Roles"].FirstOrDefault();
        var roles = string.IsNullOrWhiteSpace(rolesHeader)
            ? _developmentAuthOptions.DefaultRoles
            : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new("sub", subject),
            new(ClaimTypes.Name, name)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
