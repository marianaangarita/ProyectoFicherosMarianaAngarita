using System;
namespace ProyectoReservas.DTO;

public class ReservasDTO
{
    string localizador { get; set; } //hay un localizador unico por reserva, dentro del localizador puede haber varios pasajeros
    string paisDestino { get; set; }
    string paisOrigen { get; set; }
    string nombrePasajero { get; set; }
    string primerApellido { get; set; }
    string segundoApellido { get; set; }
    string documentoIdentidad { get; set; } //DNI, funciona como id unico de pasajero
    DateTime fechaReserva { get; set; }
    
    public ReservasDTO(string loc, string pDes, string pOri, string nomP, string pApe, string sApe, string docIden, DateTime fecha)
	{
		
        this.localizador = loc;
        this.paisDestino = pDes;
        this.paisOrigen = pOri;
        this.nombrePasajero = nomP;
        this.primerApellido = pApe;
        this.segundoApellido = sApe;
        this.documentoIdentidad = docIden;
        this.fechaReserva = fecha;
    }
}
