using System.Globalization;
using System.Text;
using AirTortilla.Web.Dto;

namespace AirTortilla.Web.Data.Csv;

/// <summary>
/// Lee el CSV de reservas (mismo formato que la entrega 1) y valida el FORMATO de cada fila.
/// Que los países existan lo comprueba el DAO contra la tabla Pais.
/// </summary>
public static class LectorCsvReservas
{
    public static async Task<(List<ReservasDTO> Filas, List<string> Avisos)> LeerAsync(Stream contenido)
    {
        var filas = new List<ReservasDTO>();
        var avisos = new List<string>();

        using var reader = new StreamReader(contenido, Encoding.UTF8);
        await reader.ReadLineAsync(); // saltar cabecera

        string? linea;
        int numeroLinea = 1;

        while ((linea = await reader.ReadLineAsync()) is not null)
        {
            numeroLinea++;
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var d = linea.Split(',');
            if (d.Length != 8)
            {
                avisos.Add($"Línea {numeroLinea}: se esperaban 8 campos y hay {d.Length}. Se omite.");
                continue;
            }

            if (!DateTime.TryParseExact(d[7].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                avisos.Add($"Línea {numeroLinea}: fecha '{d[7].Trim()}' no válida (formato yyyy-MM-dd). Se omite.");
                continue;
            }

            var localizador = d[0].Trim();
            var destino = d[1].Trim().ToUpperInvariant();
            var origen = d[2].Trim().ToUpperInvariant();
            var documento = d[6].Trim().ToUpperInvariant();

            if (localizador.Length is 0 or > 20 || documento.Length is 0 or > 20)
            {
                avisos.Add($"Línea {numeroLinea}: localizador y documento son obligatorios (máx. 20 caracteres). Se omite.");
                continue;
            }
            if (destino.Length != 2 || origen.Length != 2)
            {
                avisos.Add($"Línea {numeroLinea}: origen y destino deben ser códigos ISO de 2 letras. Se omite.");
                continue;
            }
            if (d[3].Trim().Length > 60 || d[4].Trim().Length > 60 || d[5].Trim().Length > 60)
            {
                avisos.Add($"Línea {numeroLinea}: nombre o apellidos de más de 60 caracteres. Se omite.");
                continue;
            }

            filas.Add(new ReservasDTO(localizador, destino, origen,
                d[3].Trim(), d[4].Trim(), d[5].Trim(), documento, fecha));
        }

        return (filas, avisos);
    }
}