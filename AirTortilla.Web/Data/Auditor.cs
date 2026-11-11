using System.Text.Json;
using AirTortilla.Web.Data.Entities;

namespace AirTortilla.Web.Data;

/// <summary>
/// Registra acciones en la tabla Auditoria.
/// No guarda por sí mismo: se guarda junto con el resto de cambios en el mismo SaveChanges,
/// así o se guarda todo o no se guarda nada.
/// </summary>
public static class Auditor
{
    public static void Registrar(
        AirTortillaContext db,
        int? usuarioId,
        string accion,
        string? tabla = null,
        string? registroId = null,
        object? valoresAnteriores = null,
        object? valoresNuevos = null)
    {
        db.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.Now,
            Accion = accion,
            Tabla = tabla,
            RegistroId = registroId,
            ValoresAnteriores = valoresAnteriores is null ? null : JsonSerializer.Serialize(valoresAnteriores),
            ValoresNuevos = valoresNuevos is null ? null : JsonSerializer.Serialize(valoresNuevos)
        });
    }
}