using AirTortilla.Web.Components;
using AirTortilla.Web.Data;
using AirTortilla.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===================== 1. SERVICIOS (antes de Build) =====================

// Base de datos
builder.Services.AddDbContextFactory<AirTortillaContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("AirTortilla")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'AirTortilla'. Configúrala con dotnet user-secrets.")));

// Autenticación con cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";                   // adonde se redirige si no has iniciado sesión
        options.AccessDeniedPath = "/acceso-denegado";  // sesión iniciada, pero sin permiso
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // una jornada laboral
        options.SlidingExpiration = true;               // si sigues usando la app, se renueva
        options.Cookie.HttpOnly = true;                 // el JavaScript del navegador no puede leerla
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ===================== 2. ARRANQUE: admin inicial =====================

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AirTortillaContext>>();
    await using var db = await factory.CreateDbContextAsync();

    if (!await db.Usuarios.AnyAsync())
    {
        var passwordAdmin = builder.Configuration["AdminInicial:Password"]
            ?? throw new InvalidOperationException("Falta 'AdminInicial:Password'. Configúralo con dotnet user-secrets.");

        var rolAdmin = await db.Roles.SingleAsync(r => r.Nombre == "Administrador");

        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin),
            RolId = rolAdmin.Id,
            Activo = true,
            FechaAlta = DateTime.Now
        });
        await db.SaveChangesAsync();
    }
}

// ===================== 3. PIPELINE (orden de las peticiones) =====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();   // ¿quién eres?
app.UseAuthorization();    // ¿qué puedes hacer?
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();