namespace AirTortilla.Web.Data.Exportacion;

/// <summary>Patrón Factory: decide QUÉ exportador crear según el formato pedido.</summary>
public static class ExportadorFactory
{
    public static IExportador Crear(string formato) => formato.Trim().ToLowerInvariant() switch
    {
        "csv" => new ExportadorCsv(),
        "txt" => new ExportadorTxt(),
        "json" => new ExportadorJson(),
        "xml" => new ExportadorXml(),
        _ => throw new ArgumentException($"Formato '{formato}' no soportado. Usa csv, txt, json o xml.")
    };
}