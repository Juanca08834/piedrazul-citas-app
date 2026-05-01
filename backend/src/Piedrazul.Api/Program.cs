using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Piedrazul.Api.Configuration;
using Piedrazul.Application;
using Piedrazul.Infrastructure;
using Piedrazul.Infrastructure.Persistence;
using Piedrazul.Infrastructure.Security;
using Piedrazul.Infrastructure.Seeding;
using AppAuthenticationOptions = Piedrazul.Api.Configuration.AuthenticationOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppAuthenticationOptions>(builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection("Frontend"));
builder.Services.Configure<CenterOptions>(builder.Configuration.GetSection("Center"));
builder.Services.Configure<DevelopmentAuthOptions>(builder.Configuration.GetSection("DevelopmentAuth"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var frontendBaseUrl = builder.Configuration.GetSection("Frontend").GetValue<string>("BaseUrl") ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(frontendBaseUrl)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var authenticationMode = builder.Configuration["Authentication:Mode"] ?? "Development";
if (string.Equals(authenticationMode, "Keycloak", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", false);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = KeycloakClaimsEnricher.EnrichAsync
            };
        });
}
else
{
    builder.Services
        .AddAuthentication("Development")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>("Development", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalStaff", policy => policy.RequireRole("Admin", "Scheduler", "Doctor"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(dbContext);
}

app.Run();