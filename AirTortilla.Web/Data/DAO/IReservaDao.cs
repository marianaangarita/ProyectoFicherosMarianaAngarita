using AirTortilla.Web.Dto;

namespace AirTortilla.Web.Data.Dao;

/// <summary>
/// Contrato de acceso a datos de reservas.
/// La web solo conoce esta interfaz; no sabe si debajo hay MySQL, ficheros u otra cosa.
/// </summary>
public interface IReservaDao
{
    /// <summary>Guarda en la base de datos las filas leídas de un CSV.</summary>
    Task<ResultadoImportacion> ImportarAsync(IReadOnlyList<ReservasDTO> filas, int usuarioId, int avisosLectura = 0);

    /// <summary>Lista de combinaciones destino + fecha, con su número de pasajeros (equivale a "qué ficheros hay").</summary>
    Task<List<ResumenGrupo>> ObtenerGruposAsync();

    /// <summary>Pasajeros de un destino y una fecha, ordenados por localizador y documento.</summary>
    Task<List<ReservasDTO>> ObtenerPorDestinoYFechaAsync(string destino, DateTime fecha);
}

public record ResultadoImportacion(int ReservasImportadas, int PasajerosNuevos, List<string> Avisos);

public record ResumenGrupo(string Destino, DateTime Fecha, int TotalPasajeros);