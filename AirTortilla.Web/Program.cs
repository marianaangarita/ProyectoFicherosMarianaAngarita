using AirTortilla.Web.Components;
using AirTortilla.Web.Data;
using AirTortilla.Web.Data.Dao;
using AirTortilla.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using AirTortilla.Web.Data.Exportacion;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===================== 1. SERVICIOS (antes de Build) =====================

// Base de datos
builder.Services.AddDbContextFactory<AirTortillaContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("AirTortilla")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'AirTortilla'. Configúrala con dotnet user-secrets.")));

// Acceso a datos(patrón DAO): las páginas piden IReservaDao y aquí se decide la implementación
builder.Services.AddScoped<IReservaDao, ReservaDaoMySql>();

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

// Cerrar sesión: borra la cookie y vuelve al login
app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// Exportar: genera el fichero AirTortilla_XX_YYYY_MM_DD.<ext> y lo descarga
app.MapGet("/exportar/{destino}/{fecha}/{formato}", async (
    string destino, DateTime fecha, string formato,
    IReservaDao reservaDao, IDbContextFactory<AirTortillaContext> dbFactory, HttpContext context) =>
{
    IExportador exportador;
    try
    {
        exportador = ExportadorFactory.Crear(formato);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    destino = destino.ToUpperInvariant();
    var filas = await reservaDao.ObtenerPorDestinoYFechaAsync(destino, fecha);
    if (filas.Count == 0) return Results.NotFound("No hay reservas para ese destino y fecha.");

    var nombreFichero = $"AirTortilla_{destino}_{fecha:yyyy_MM_dd}.{exportador.Extension}";

    // Trazabilidad: quién exportó qué
    await using var db = await dbFactory.CreateDbContextAsync();
    var usuarioId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    Auditor.Registrar(db, usuarioId, "EXPORTAR", "reserva",
        valoresNuevos: new { Fichero = nombreFichero, Registros = filas.Count });
    await db.SaveChangesAsync();

    return Results.File(exportador.Exportar(filas), exportador.TipoMime, nombreFichero);
}).RequireAuthorization();

app.Run();

