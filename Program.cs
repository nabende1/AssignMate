using AssignMate.Components;
using AssignMate.Data;
using AssignMate.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider")?.Trim();
var sqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServerConnection")?.Trim().Trim('"');
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim().Trim('"');
var connectionString = databaseProvider?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true
    ? sqlServerConnectionString ?? defaultConnectionString
    : defaultConnectionString;

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Data Source=assignmate.db";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();
builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<TaskStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapPost("/account/login", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "true";

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=missing");
    }

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        return Results.Redirect("/dashboard");
    }

    if (result.IsLockedOut)
    {
        return Results.Redirect("/login?error=locked");
    }

    return Results.Redirect("/login?error=invalid");
}).WithMetadata(new RequireAntiforgeryTokenAttribute());
app.MapPost("/account/register", async (HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString().Trim();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/register?error=missing");
    }

    if (!new EmailAddressAttribute().IsValid(email))
    {
        return Results.Redirect("/register?error=invalid-email");
    }

    if (password.Length < 8)
    {
        return Results.Redirect("/register?error=password");
    }

    var existingUser = await userManager.FindByEmailAsync(email);
    if (existingUser is not null)
    {
        return Results.Redirect("/register?error=duplicate");
    }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FullName = name,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        var duplicate = result.Errors.Any(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) || error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase));
        return Results.Redirect(duplicate ? "/register?error=duplicate" : "/register?error=invalid");
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Redirect("/dashboard");
}).WithMetadata(new RequireAntiforgeryTokenAttribute());
app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).WithMetadata(new RequireAntiforgeryTokenAttribute());
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await PrepareLegacySqliteDatabaseAsync(database);
    await database.Database.MigrateAsync();
}

static async Task PrepareLegacySqliteDatabaseAsync(ApplicationDbContext database)
{
    if (!database.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        return;
    }

    var connection = database.Database.GetDbConnection();
    await connection.OpenAsync();
    if (!await TableExistsAsync(connection, "AspNetUsers") || await MigrationHistoryExistsAsync(connection))
    {
        return;
    }

    // The first development database predates migrations. Baseline it without dropping local accounts or tasks.
    if (!await ColumnExistsAsync(connection, "Tasks", "CreatedAtUtc"))
    {
        await ExecuteAsync(connection, "ALTER TABLE Tasks ADD COLUMN CreatedAtUtc TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'");
    }
    if (!await ColumnExistsAsync(connection, "Tasks", "UpdatedAtUtc"))
    {
        await ExecuteAsync(connection, "ALTER TABLE Tasks ADD COLUMN UpdatedAtUtc TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'");
    }

    await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL)");
    await ExecuteAsync(connection, "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260811093653_InitialCreate', '10.0.10')");
}

static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);
    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
}

static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA table_info([{tableName}])";
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase)) return true;
    }
    return false;
}

static async Task<bool> MigrationHistoryExistsAsync(DbConnection connection)
{
    if (!await TableExistsAsync(connection, "__EFMigrationsHistory")) return false;
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
    return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
}

static async Task ExecuteAsync(DbConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

app.Run();

public partial class Program { }
