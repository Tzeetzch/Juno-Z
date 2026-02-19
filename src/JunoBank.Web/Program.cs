using JunoBank.Application.Interfaces;
using JunoBank.Application.Services;
using JunoBank.Infrastructure.BackgroundServices;
using JunoBank.Infrastructure.Data;
using JunoBank.Infrastructure.Email;
using JunoBank.Web.Auth;
using JunoBank.Web.Components;
using JunoBank.Web.Services;
using JunoBank.Domain.Entities;
using JunoBank.Domain.Enums;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Load email config from data volume (written by setup wizard, persists across restarts)
// Derive path from connection string so it matches EmailConfigService.GetConfigPath() on all platforms
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Data/junobank.db";
var dataDir = Path.GetDirectoryName(connStr.Replace("Data Source=", "")) ?? "Data";
var emailConfigPath = Path.Combine(dataDir, "email-config.json");
builder.Configuration.AddJsonFile(emailConfigPath, optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddMudServices();

// Database - use JUNO_TEST_DB env var if set (for isolated test runs), otherwise use config
var testDbName = Environment.GetEnvironmentVariable("JUNO_TEST_DB");
var connectionString = string.IsNullOrEmpty(testDbName)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : $"Data Source=Data/{testDbName}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("JunoBank.Infrastructure")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Authentication - hardened cookie configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
}).AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IAuthStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

// Application services
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAllowanceService, AllowanceService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ISetupService, SetupService>();
builder.Services.AddScoped<IEmailConfigService, EmailConfigService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBrowserTimeService, BrowserTimeService>();

// Email service - checks config at runtime so setup wizard changes take effect immediately
builder.Services.AddScoped<SmtpEmailService>();
builder.Services.AddScoped<ConsoleEmailService>();
builder.Services.AddScoped<IEmailService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var emailHost = config.GetValue<string>("Email:Host");
    if (!string.IsNullOrEmpty(emailHost))
        return sp.GetRequiredService<SmtpEmailService>();
    return sp.GetRequiredService<ConsoleEmailService>();
});

// Background services
builder.Services.AddHostedService<AllowanceBackgroundService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

// Data Protection - use custom path if specified (for Docker)
var dataProtectionKeysPath = builder.Configuration["DataProtection:Keys"];
if (!string.IsNullOrEmpty(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("JunoBank");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers middleware
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedAsync(context, passwordService);
}

// CLI: emergency password reset
// Usage: dotnet JunoBank.Web.dll reset-password user@email.com newpassword
if (args.Length >= 3 && args[0] == "reset-password")
{
    var email = args[1].ToLowerInvariant();
    var newPassword = args[2];

    if (newPassword.Length < 8)
    {
        Console.Error.WriteLine("Error: Password must be at least 8 characters.");
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pwdService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email && u.Role == UserRole.Parent);

    if (user == null)
    {
        Console.Error.WriteLine($"Error: No parent account found with email '{email}'.");
        return;
    }

    user.PasswordHash = pwdService.HashPassword(newPassword);
    user.FailedLoginAttempts = 0;
    user.LockoutUntil = null;
    await db.SaveChangesAsync();

    Console.WriteLine($"Password reset successfully for {user.Name} ({user.Email}).");
    Console.WriteLine("Lockout state cleared.");
    return;
}

app.Run();
