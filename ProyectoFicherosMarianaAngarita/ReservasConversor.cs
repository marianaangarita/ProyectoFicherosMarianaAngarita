using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProyectoReservas.DTO;

namespace ProyectoReservas.Conversor;

 public class ReservasConversor
 {
    public static List<ReservasDTO> LeerReservasDesdeCsv(string rutaArchivo)
    {
        List<ReservasDTO> listaReservas = new List<ReservasDTO>();

        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine("El archivo no existe.");
                return listaReservas;
        }

        using (StreamReader reader = new StreamReader(rutaArchivo))
        {
            // 1. Saltar la línea de cabecera
            string cabecera = reader.ReadLine();

            string linea;

            while((linea = reader.ReadLine()) !=null)
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                string[] datos = linea.Split(',');

                string localizador = datos[0];
                string paisDestino = datos[1];
                string paisOrigen = datos[2];
                string nombrePasajero = datos[3];
                string primerApellido = datos[4];
                string segundoApellido = datos[5];
                string documentoIdentidad = datos[6];

                // Parsear la fecha desde string (yyyy-MM-dd) a DateTime
                DateTime fechaReserva = DateTime.Parse(datos[7]);

                ReservasDTO reserva = new ReservasDTO(
                    localizador,
                    paisDestino,
                    paisOrigen,
                    nombrePasajero,
                    primerApellido,
                    segundoApellido,
                    documentoIdentidad,
                    fechaReserva
                    );
                
                listaReservas.Add( reserva );
            
            }

            return listaReservas;
        }
    }

        //    public static 
        //    // Agrupamos la lista por el país de destino
        //    var reservasAgrupadasPorPais = listaReservas.GroupBy(r => r.PaisDestino);

        //foreach (var grupoPais in reservasAgrupadasPorPais)
        //{
            //    string pais = grupoPais.Key; // Ejemplo: "FR"

        //    // Dentro de este país, ordenamos por fecha
        //    var reservasOrdenadas = grupoPais.OrderBy(r => r.FechaReserva).ToList();

         // Aquí ya podríamos separar por fechas y crear los archivos AirTortilla_FR_YYYY_MM_DD...
        //}
 }

