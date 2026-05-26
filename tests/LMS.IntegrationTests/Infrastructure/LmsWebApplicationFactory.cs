using System.Text;
using LMS.Infrastructure.Auth;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LMS.IntegrationTests.Infrastructure;

public class LmsWebApplicationFactory : WebApplicationFactory<Program>
{
    // One constant key used both to SIGN tokens (JwtService override) and to VALIDATE
    // them (JwtBearerOptions override). Having both sides read the same source ensures
    // they always agree, regardless of user-secrets or environment-specific config.
    internal const string TestJwtKey = "test-jwt-signing-key-must-be-at-least-32-chars!";
    internal const string TestJwtIssuer = "lms-api";
    internal const string TestJwtAudience = "lms-frontend";

    // A single shared connection kept open for the factory lifetime.
    // SQLite in-memory databases are destroyed as soon as the last connection to
    // them closes, so we must hold this open to keep the database alive across
    // all HTTP requests made during a test.
    private readonly SqliteConnection _connection;

    public LmsWebApplicationFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            // Provide a complete, self-contained JWT config so no other source
            // (user-secrets, environment variables) can affect token signing.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Database ──────────────────────────────────────────────────────
            // EF Core 8+ stores the provider setup in IDbContextOptionsConfiguration<T>
            // in addition to DbContextOptions<T>. Both must be removed, otherwise two
            // providers (SqlServer + Sqlite) end up in the same service provider and EF
            // Core throws at startup.
            var optionsConfig = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<LmsDbContext>));
            if (optionsConfig is not null)
            {
                services.Remove(optionsConfig);
            }

            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LmsDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }

            // Replace with the shared SQLite connection so all requests within
            // one factory instance hit the same in-memory database.
            services.AddDbContext<LmsDbContext>(opt =>
                opt.UseSqlite(_connection));

            // ── JWT signing (token GENERATION) ────────────────────────────────
            // Replace the registered JwtService with one backed by a fixed test
            // IConfiguration so token generation always uses TestJwtKey,
            // regardless of what IConfiguration ultimately resolves to.
            var jwtServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(JwtService));
            if (jwtServiceDescriptor is not null)
            {
                services.Remove(jwtServiceDescriptor);
            }

            services.AddScoped<JwtService>(sp =>
            {
                var testConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = TestJwtKey,
                        ["Jwt:Issuer"] = TestJwtIssuer,
                        ["Jwt:Audience"] = TestJwtAudience,
                        ["Jwt:ExpirationMinutes"] = "60"
                    })
                    .Build();
                return new JwtService(testConfig, sp.GetRequiredService<UserManager<ApplicationUser>>());
            });

            // ── JWT validation (token VERIFICATION) ───────────────────────────
            // Remove all existing Configure/PostConfigure registrations for
            // JwtBearerOptions so the validation parameters are fully controlled
            // by the test and cannot be overridden by environment-based config.
            var jwtOptionRegistrations = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>)
                         || d.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>))
                .ToList();
            foreach (var d in jwtOptionRegistrations)
            {
                services.Remove(d);
            }

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestJwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestJwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestJwtKey))
                };
            });
        });

        // "Integration" prevents user-secrets from loading (they only load in Development).
        // Without this, user-secrets would override our AddInMemoryCollection JWT key,
        // causing a mismatch between JwtService (signing) and JWT bearer (validation).
        builder.UseEnvironment("Integration");
    }

    // We override Dispose(bool) — the protected virtual overload that the
    // IDisposable pattern routes through — to close our connection after the
    // ASP.NET Core host has fully shut down (base.Dispose first).
    // disposing = true  → called from IDisposable.Dispose(); safe to release managed resources.
    // disposing = false → called from finalizer; managed objects may be gone, skip them.
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
