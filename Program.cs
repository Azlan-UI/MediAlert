using MediAlert;
using MediAlert.Components;
using MediAlert.Configuration;
using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.Models;
using MediAlert.Services.Auth;
using MediAlert.Services.Billing;
using MediAlert.Services.Compliance;
using MediAlert.Services.OpenFda;
using MediAlert.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

// ═══════════════════════════════════════════════════════════════════════════
// Program.cs — The application's entry point and composition root.
//
// WHAT HAPPENS HERE:
// 1. We build the dependency injection container (register all services)
// 2. We configure the middleware pipeline (what runs on every HTTP request)
// 3. We run startup tasks (migrations, seeding)
// 4. We start the web server
//
// ARCHITECTURE PRINCIPLE — Composition Root:
// All "wiring" (which interface maps to which implementation) happens here.
// Nothing inside services or controllers should call `new` on a dependency.
// ═══════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// ─── 1. CONFIGURATION ────────────────────────────────────────────────────────
// Load JWT settings into a strongly-typed class.
// Services inject IOptions<JwtSettings> to access these values.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<StripeSettings>(options =>
{
    builder.Configuration.GetSection("StripeSettings").Bind(options);

    var stripeSection = builder.Configuration.GetSection("Stripe");
    options.SecretKey = FirstConfigured(stripeSection["SecretKey"], options.SecretKey);
    options.PublishableKey = FirstConfigured(stripeSection["PublishableKey"], options.PublishableKey);
    options.WebhookSecret = FirstConfigured(stripeSection["WebhookSecret"], options.WebhookSecret);
    options.ProductId = FirstConfigured(stripeSection["ProductId"], options.ProductId);
    options.PriceId = FirstConfigured(stripeSection["PriceId"], options.PriceId);
});

static string FirstConfigured(string? preferred, string fallback) =>
    string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

builder.Services.AddOpenFdaIntegration(builder.Configuration);
builder.Services.AddComplianceServices();
builder.Services.AddModule6To10Services();

// ─── 2. DATABASE — Entity Framework Core + PostgreSQL ────────────────────────
// Npgsql is the PostgreSQL driver for EF Core.
// "DefaultConnection" comes from appsettings.json.
// The connection string contains the host, database name, username, password.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            // Retry on transient connection failures (network blips).
            // Healthcare apps need resilience.
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));

// ─── 3. ASP.NET CORE IDENTITY ────────────────────────────────────────────────
// Identity manages user storage, password hashing, role assignment,
// lockout, and token generation.
//
// WHY AddIdentity vs AddIdentityCore?
// AddIdentityCore is lighter (no cookie auth, no UI defaults).
// We use AddIdentityCore because we're doing JWT auth, not cookie auth.
// We manually add what we need.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // ── Password Policy (NFR-03, FR-01) ──────────────────────────────
        // These enforce the minimum password requirements.
        // FR-01 rejection criteria: weak passwords < 8 chars must be rejected.
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;  // Special char required

        // ── Lockout Policy (Brute-force protection) ───────────────────────
        // After 5 failed attempts, lock the account for 15 minutes.
        // This is ASP.NET Core Identity's built-in protection.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // ── User Settings ─────────────────────────────────────────────────
        // Each email can only register ONE account.
        options.User.RequireUniqueEmail = true;

        // ── Sign-in Settings ──────────────────────────────────────────────
        // We handle email verification manually in AuthService
        // (via IsEmailVerified flag). We set this to false here so
        // Identity doesn't block signIn — our service does the check instead.
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<IdentityRole>()              // Enables role management
    .AddEntityFrameworkStores<ApplicationDbContext>()  // Use our DbContext
    .AddDefaultTokenProviders();           // For password reset tokens etc.

// ─── 4. JWT AUTHENTICATION ───────────────────────────────────────────────────
// Configure ASP.NET Core to validate JWT tokens on every request.
// When a request arrives with "Authorization: Bearer {token}",
// this middleware validates the token and populates HttpContext.User
// with the claims from the token.
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JwtSettings configuration section is missing. " +
        "Add it to appsettings.json or User Secrets.");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) ||
    jwtSettings.SecretKey == "REPLACE_WITH_USER_SECRETS_DO_NOT_COMMIT")
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "JWT SecretKey must be configured in production. Use environment variables.");
    }
    // In dev, warn but continue.
    Console.WriteLine("⚠️  WARNING: Using placeholder JWT SecretKey. " +
                      "Update appsettings.Development.json before testing.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services
    .AddAuthentication(options =>
    {
        // Make JWT the default scheme for both authentication and challenge.
        // "Challenge" = what happens when an unauthenticated request hits
        //               [Authorize] endpoint → returns 401, not redirect to login page.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // VALIDATE SIGNATURE — Was this token signed by our secret key?
            // This is the core security check. A tampered token fails here.
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            // VALIDATE ISSUER — Was this token issued by MediAlert?
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            // VALIDATE AUDIENCE — Is this token intended for MediAlert users?
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            // VALIDATE EXPIRY — Has the token expired?
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,  // No tolerance — exact expiry.
                                        // Set to TimeSpan.FromMinutes(5) for
                                        // distributed systems with clock drift.
        };

        // Log authentication failures for debugging.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}",
                    context.Exception.Message);
                return Task.CompletedTask;
            },
        };
    });

// ─── 5. AUTHORIZATION ────────────────────────────────────────────────────────
// [Authorize] on controllers/actions uses these policies.
// [Authorize(Roles = "Admin")] reads the "role" claim from the JWT.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.PremiumPatient, policy =>
    {
        policy.RequireRole(UserRoles.Patient);
        policy.AddRequirements(new PremiumAccessRequirement());
    });
});

// ─── 6. OUR SERVICES (Dependency Injection) ──────────────────────────────────
// Register our custom services so the DI container can inject them.
//
// LIFETIMES:
// Scoped   = new instance per HTTP request (most services — access DB in same scope)
// Singleton = one instance for app lifetime (stateless services only)
// Transient = new instance every time injected (rare)
//
// AuthService is Scoped because it uses UserManager which is Scoped.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, MediAlert.Providers.JwtAuthenticationStateProvider>();

// ─── 7. CONTROLLERS ──────────────────────────────────────────────────────────
// Registers all classes decorated with [ApiController].
// This also enables automatic model validation (DataAnnotations on DTOs).
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});

// ─── 8. BLAZOR ───────────────────────────────────────────────────────────────
// Adds Blazor Server/WebAssembly support (keep your existing Blazor setup).
// DO NOT remove this — your teammates' Blazor pages depend on it.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── 9. SWAGGER / OPENAPI ────────────────────────────────────────────────────
// Swagger generates interactive API documentation at /swagger.
// Invaluable for testing endpoints and sharing API docs with teammates.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediAlert API",
        Version = "v1",
        Description = "Healthcare medication management platform API",
    });

    // Add JWT authentication to Swagger UI.
    // This lets you paste a token into Swagger and test protected endpoints.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci...",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

// ─── 10. CORS (Cross-Origin Resource Sharing) ────────────────────────────────
// If your Blazor frontend runs on a different port than the API during dev,
// you need CORS. For a single Blazor project this is less critical,
// but good to configure for Postman testing and future frontend separation.
builder.Services.AddCors(options =>
{
    options.AddPolicy("MediAlertDev", policy =>
    {
        policy
            .WithOrigins("https://localhost:7000", "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ─── 11. BACKGROUND SERVICES ───────────────────────────────────────────────────
builder.Services.AddHostedService<MediAlert.Services.Background.MediAlertBackgroundWorker>();

// ═══════════════════════════════════════════════════════════════════════════
// BUILD THE APP
// Everything above was registering services.
// Everything below configures the HTTP pipeline.
// ═══════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// ─── STARTUP TASKS: Migrations + Seeding ─────────────────────────────────────
// Run database migrations automatically on startup.
//
// IMPORTANT TEAM RULE:
// Only ONE developer owns adding migrations.
// NEVER run `dotnet ef database update` manually — the app auto-migrates.
// Coordinate with teammates before adding a new migration.
//
// WHY AUTO-MIGRATE?
// For a 1-week sprint, auto-migration on startup is the safest approach.
// No teammate needs to manually run migration scripts.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        // Seed roles and default admin
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await DatabaseSeeder.SeedAsync(roleManager, userManager, logger);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex,
            "An error occurred during database migration or seeding. " +
            "Verify your PostgreSQL connection string and that the server is running.");
        throw; // Fail fast — don't start the app with a broken database.
    }
}

// ─── HTTP PIPELINE CONFIGURATION ─────────────────────────────────────────────
// ORDER MATTERS. Middleware runs top to bottom.
// A request passes through each middleware in sequence.

if (app.Environment.IsDevelopment())
{
    // Show detailed error pages in development.
    app.UseDeveloperExceptionPage();

    // Swagger UI: visit https://localhost:{port}/swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MediAlert API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
    });
}
else
{
    // In production, use our global error handler (Phase 2 enhancement).
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();  // Serve wwwroot files (Blazor WASM, CSS, etc.)

app.UseRouting();      // MUST come before Auth and Antiforgery

app.UseCors("MediAlertDev");

// CRITICAL ORDER: Authentication MUST come before Authorization.
// Authentication = "Who are you?" (reads the JWT, populates HttpContext.User)
// Authorization  = "Are you allowed?" (checks [Authorize] attributes)
app.UseAuthentication();
app.UseAuthorization();

// Antiforgery middleware is REQUIRED by Blazor's MapRazorComponents endpoint.
// It MUST be placed after UseAuthorization() and before any MapXxx() call.
// API controllers are unaffected — they have IgnoreAntiforgeryTokenAttribute
// applied globally in AddControllers(), so Postman/JWT calls work fine.
app.UseAntiforgery();

// Map API controllers (handles /api/... routes)
app.MapControllers();

// Map Blazor components (handles all non-API routes)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
