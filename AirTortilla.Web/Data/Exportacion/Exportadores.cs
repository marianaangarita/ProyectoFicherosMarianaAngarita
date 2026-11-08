using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using AirTortilla.Web.Dto;

namespace AirTortilla.Web.Data.Exportacion;

/// <summary>Contrato común: cada formato sabe convertir las reservas en bytes.</summary>
public interface IExportador
{
    string Extension { get; }
    string TipoMime { get; }
    byte[] Exportar(List<ReservasDTO> filas);
}

public class ExportadorCsv : IExportador
{
    public string Extension => "csv";
    public string TipoMime => "text/csv";

    public byte[] Exportar(List<ReservasDTO> filas)
    {
        var lineas = new List<string> { ReservasDTO.GetCsvHeader() };
        lineas.AddRange(filas.Select(r => r.ToCsvLine()));
        return Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lineas));
    }
}

public class ExportadorTxt : IExportador
{
    public string Extension => "txt";
    public string TipoMime => "text/plain";

    public byte[] Exportar(List<ReservasDTO> filas) =>
        Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, filas.Select(r => r.ToTxtLine())));
}

public class ExportadorJson : IExportador
{
    public string Extension => "json";
    public string TipoMime => "application/json";

    public byte[] Exportar(List<ReservasDTO> filas) =>
        JsonSerializer.SerializeToUtf8Bytes(filas, new JsonSerializerOptions { WriteIndented = true });
}

public class ExportadorXml : IExportador
{
    public string Extension => "xml";
    public string TipoMime => "application/xml";

    public byte[] Exportar(List<ReservasDTO> filas)
    {
        var serializer = new XmlSerializer(typeof(List<ReservasDTO>));
        using var memoria = new MemoryStream();
        serializer.Serialize(memoria, filas);
        return memoria.ToArray();
    }
}