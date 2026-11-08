using AirTortilla.Web.Data;
using Microsoft.EntityFrameworkCore;
using AirTortilla.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AirTortillaContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("AirTortilla")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'AirTortilla'. Configúrala con dotnet user-secrets.")));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
