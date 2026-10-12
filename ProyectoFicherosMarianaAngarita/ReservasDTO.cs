using System;
namespace ProyectoReservas.DTO;

public class ReservasDTO
{
    public string Localizador { get; set; } //hay un localizador unico por reserva, dentro del localizador puede haber varios pasajeros
    public string Destino { get; set; }
    public string Origen { get; set; }
    public string NombrePasajero { get; set; }
    public string PrimerApellido { get; set; }
    public string SegundoApellido { get; set; }
    public string DocumentoIdentidad { get; set; } //DNI, funciona como id unico de pasajero
    public DateTime FechaReserva { get; set; }

    // Constructor vacío requerido por el serializador XML de C#
    public ReservasDTO() { }

    public ReservasDTO(string loc, string pDes, string pOri, string nomP, string pApe, string sApe, string docIden, DateTime fecha)
	{
		
        this.Localizador = loc;
        this.Destino = pDes;
        this.Origen = pOri;
        this.NombrePasajero = nomP;
        this.PrimerApellido = pApe;
        this.SegundoApellido = sApe;
        this.DocumentoIdentidad = docIden;
        this.FechaReserva = fecha;
    }

    public static string GetCsvHeader()
    {
        return "Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva";
    }

    public string ToCsvLine()
    {
        return $"{Localizador},{Destino},{Origen},{NombrePasajero},{PrimerApellido},{SegundoApellido},{DocumentoIdentidad},{FechaReserva:yyyy-MM-dd}";
    }

    public string ToTxtLine()
    {
        return $"{Localizador} | {Destino} | {Origen} | {NombrePasajero} | {PrimerApellido} | {SegundoApellido} | {DocumentoIdentidad} | {FechaReserva:yyyy-MM-dd}";
    }


}
