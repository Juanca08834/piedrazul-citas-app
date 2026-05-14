using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Piedrazul.Infrastructure.Security;
using Xunit;

namespace Piedrazul.Domain.Tests;

public sealed class DevelopmentAuthHandlerTests
{
    // ── Guarda de entorno ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAuthentication_DebeImpedirAcceso_CuandoElEntornoNoEsDesarrollo()
    {
        var handler = CreateHandler(isDevelopment: false);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(NewScheme(), context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.AuthenticateAsync());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task HandleAuthentication_DebeImpedirAcceso_EnCualquierEntornoNoDesarrollo(string envName)
    {
        var handler = CreateHandler(environmentName: envName);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(NewScheme(), context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.AuthenticateAsync());
    }

    [Fact]
    public async Task HandleAuthentication_DebeAutenticarExitosamente_EnEntornoDesarrollo()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
    }

    // ── Valores por defecto ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAuthentication_DebeUsarValoresPorDefecto_CuandoNoHayCabeceras()
    {
        var opts = new DevelopmentAuthOptions
        {
            DefaultSubject = "sujeto-prueba",
            DefaultName    = "Usuario Prueba",
            DefaultEmail   = "prueba@test.com",
            DefaultRoles   = ["Patient"],
        };
        var handler = CreateHandler(isDevelopment: true, opts);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();
        var principal = result.Principal!;

        Assert.Equal("sujeto-prueba", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Usuario Prueba", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("prueba@test.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.True(principal.IsInRole("Patient"));
    }

    [Fact]
    public async Task HandleAuthentication_NoDebeIncluirClaimEmail_CuandoDefaultEmailEsNulo()
    {
        var opts = new DevelopmentAuthOptions { DefaultEmail = null };
        var handler = CreateHandler(isDevelopment: true, opts);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();

        Assert.Null(result.Principal!.FindFirstValue(ClaimTypes.Email));
    }

    // ── Cabeceras X-Debug ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAuthentication_DebeSobreescribirSubject_CuandoHayCabeceraXDebugSubject()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Debug-Subject"] = "sujeto-personalizado";
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();

        Assert.Equal("sujeto-personalizado", result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("sujeto-personalizado", result.Principal!.FindFirstValue("sub"));
    }

    [Fact]
    public async Task HandleAuthentication_DebeSobreescribirRoles_CuandoHayCabeceraXDebugRoles()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Debug-Roles"] = "Admin";
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();
        var principal = result.Principal!;

        Assert.True(principal.IsInRole("Admin"));
        Assert.False(principal.IsInRole("Patient")); // rol por defecto no debe aplicarse
    }

    [Fact]
    public async Task HandleAuthentication_DebeAsignarMultiplesRoles_CuandoLaCabeceraContieneListaSeparadaPorComas()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Debug-Roles"] = "Admin, Doctor, Scheduler";
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();
        var principal = result.Principal!;

        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("Doctor"));
        Assert.True(principal.IsInRole("Scheduler"));
    }

    [Fact]
    public async Task HandleAuthentication_DebeIgnorarEspacios_EnLaCabeceraDeRoles()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Debug-Roles"] = "  Admin  ,  Doctor  ";
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();
        var principal = result.Principal!;

        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("Doctor"));
    }

    [Fact]
    public async Task HandleAuthentication_DebeSobreescribirEmail_CuandoHayCabeceraXDebugEmail()
    {
        var handler = CreateHandler(isDevelopment: true);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Debug-Email"] = "otro@correo.com";
        await handler.InitializeAsync(NewScheme(), context);

        var result = await handler.AuthenticateAsync();

        Assert.Equal("otro@correo.com", result.Principal!.FindFirstValue(ClaimTypes.Email));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AuthenticationScheme NewScheme() =>
        new("Dev", "Dev", typeof(DevelopmentAuthHandler));

    private static DevelopmentAuthHandler CreateHandler(
        bool isDevelopment,
        DevelopmentAuthOptions? options = null) =>
        CreateHandler(isDevelopment ? "Development" : "Production", options);

    private static DevelopmentAuthHandler CreateHandler(
        string environmentName,
        DevelopmentAuthOptions? options = null)
    {
        var opts = options ?? new DevelopmentAuthOptions();
        return new DevelopmentAuthHandler(
            new FakeOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(opts),
            new FakeHostEnvironment(environmentName));
    }

    private sealed class FakeOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
