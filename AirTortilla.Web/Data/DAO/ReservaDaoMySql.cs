using AirTortilla.Web.Data.Entities;
using AirTortilla.Web.Dto;
using Microsoft.EntityFrameworkCore;

namespace AirTortilla.Web.Data.Dao;

/// <summary>
/// Implementación del DAO de reservas sobre MySQL con EF Core.
/// Traduce entre el DTO plano (una fila por pasajero) y el modelo normalizado (Reserva, Pasajero, ReservaPasajero).
/// </summary>
public class ReservaDaoMySql(IDbContextFactory<AirTortillaContext> dbFactory) : IReservaDao
{
    public async Task<ResultadoImportacion> ImportarAsync(IReadOnlyList<ReservasDTO> filas, int usuarioId)
    {
        var avisos = new List<string>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // 1. Cargar de golpe lo que necesitamos comprobar (evita una consulta por fila)
        var paises = (await db.Paises.Select(p => p.CodigoIso).ToListAsync()).ToHashSet();

        var localizadores = filas.Select(f => f.Localizador).Distinct().ToList();
        var localizadoresExistentes = (await db.Reservas
            .Where(r => localizadores.Contains(r.Localizador))
            .Select(r => r.Localizador)
            .ToListAsync()).ToHashSet();

        var documentos = filas.Select(f => f.DocumentoIdentidad).Distinct().ToList();
        var pasajeros = await db.Pasajeros
            .Where(p => documentos.Contains(p.DocumentoIdentidad))
            .ToDictionaryAsync(p => p.DocumentoIdentidad);

        await using var transaccion = await db.Database.BeginTransactionAsync();

        var reservasNuevas = new List<Reserva>();
        int pasajerosNuevos = 0;

        // 2. Una reserva por localizador
        foreach (var grupo in filas.GroupBy(f => f.Localizador))
        {
            var primera = grupo.First();

            if (localizadoresExistentes.Contains(grupo.Key))
            {
                avisos.Add($"Localizador {grupo.Key}: ya existe en la base de datos. Se omite.");
                continue;
            }
            if (grupo.Any(f => f.Origen != primera.Origen || f.Destino != primera.Destino || f.FechaReserva != primera.FechaReserva))
            {
                avisos.Add($"Localizador {grupo.Key}: sus pasajeros tienen distinto origen, destino o fecha. Se omite.");
                continue;
            }
            if (!paises.Contains(primera.Origen) || !paises.Contains(primera.Destino))
            {
                avisos.Add($"Localizador {grupo.Key}: código ISO no válido (origen '{primera.Origen}', destino '{primera.Destino}'). Se omite.");
                continue;
            }

            var reserva = new Reserva
            {
                Localizador = grupo.Key,
                Origen = primera.Origen,
                Destino = primera.Destino,
                FechaReserva = primera.FechaReserva.Date
            };

            foreach (var fila in grupo.DistinctBy(f => f.DocumentoIdentidad))
            {
                if (!pasajeros.TryGetValue(fila.DocumentoIdentidad, out var pasajero))
                {
                    pasajero = new Pasajero
                    {
                        DocumentoIdentidad = fila.DocumentoIdentidad,
                        Nombre = fila.NombrePasajero,
                        PrimerApellido = fila.PrimerApellido,
                        SegundoApellido = string.IsNullOrWhiteSpace(fila.SegundoApellido) ? null : fila.SegundoApellido
                    };
                    pasajeros[fila.DocumentoIdentidad] = pasajero;   // si sale en otra reserva del CSV, se reutiliza
                    pasajerosNuevos++;
                }
                reserva.Pasajeros.Add(pasajero);   // EF crea la fila en ReservaPasajero
            }

            if (grupo.Count() != reserva.Pasajeros.Count)
                avisos.Add($"Localizador {grupo.Key}: documento repetido dentro de la misma reserva. Se ha guardado una sola vez.");

            db.Reservas.Add(reserva);
            reservasNuevas.Add(reserva);
        }

        await db.SaveChangesAsync();   // MySQL asigna los Id

        // 3. Auditoría: una fila por reserva creada + un resumen de la importación
        foreach (var r in reservasNuevas)
        {
            Auditor.Registrar(db, usuarioId, "INSERT", "reserva", r.Id.ToString(), valoresNuevos: new
            {
                r.Localizador,
                r.Origen,
                r.Destino,
                Fecha = r.FechaReserva.ToString("yyyy-MM-dd"),
                Pasajeros = r.Pasajeros.Select(p => p.DocumentoIdentidad)
            });
        }
        Auditor.Registrar(db, usuarioId, "IMPORTAR", "reserva", valoresNuevos: new
        {
            FilasValidas = filas.Count,
            ReservasImportadas = reservasNuevas.Count,
            PasajerosNuevos = pasajerosNuevos,
            Avisos = avisos.Count
        });
        await db.SaveChangesAsync();

        await transaccion.CommitAsync();

        return new ResultadoImportacion(reservasNuevas.Count, pasajerosNuevos, avisos);
    }

    public async Task<List<ResumenGrupo>> ObtenerGruposAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var grupos = await db.Reservas
            .SelectMany(r => r.Pasajeros, (r, p) => new { r.Destino, r.FechaReserva })
            .GroupBy(x => new { x.Destino, x.FechaReserva })
            .Select(g => new { g.Key.Destino, g.Key.FechaReserva, Total = g.Count() })
            .OrderBy(g => g.Destino).ThenBy(g => g.FechaReserva)
            .ToListAsync();

        return grupos.Select(g => new ResumenGrupo(g.Destino, g.FechaReserva, g.Total)).ToList();
    }

    public async Task<List<ReservasDTO>> ObtenerPorDestinoYFechaAsync(string destino, DateTime fecha)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var dia = fecha.Date;

        var filas = await db.Reservas
            .Where(r => r.Destino == destino && r.FechaReserva == dia)
            .SelectMany(r => r.Pasajeros, (r, p) => new { r, p })
            .OrderBy(x => x.r.Localizador).ThenBy(x => x.p.DocumentoIdentidad)
            .Select(x => new
            {
                x.r.Localizador,
                x.r.Destino,
                x.r.Origen,
                x.p.Nombre,
                x.p.PrimerApellido,
                x.p.SegundoApellido,
                x.p.DocumentoIdentidad,
                x.r.FechaReserva
            })
            .ToListAsync();

        return filas.Select(f => new ReservasDTO(f.Localizador, f.Destino, f.Origen, f.Nombre,
            f.PrimerApellido, f.SegundoApellido ?? "", f.DocumentoIdentidad, f.FechaReserva)).ToList();
    }
}