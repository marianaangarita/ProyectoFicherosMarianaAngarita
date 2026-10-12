using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using ProyectoReservas.DTO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace ProyectoReservas.Conversor;

 public class ReservasConversor
 {
    #region Leer Fichero CSV
    public static List<ReservasDTO> LeerReservasDesdeCsv(string rutaArchivo)
    {
        List<ReservasDTO> listaReservas = new List<ReservasDTO>();

        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine("El archivo no existe.");
            return listaReservas;
        }

        try
        {
            using (StreamReader reader = new StreamReader(rutaArchivo))
            {
                // 1. Saltar la línea de cabecera
                string cabecera = reader.ReadLine();
                string linea;
                int numeroLinea = 1;

                while ((linea = reader.ReadLine()) != null)
                {
                    numeroLinea++; //esto porque ya no se añade?

                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    try
                    {
                        string[] datos = linea.Split(',');

                        if (datos.Length < 8)
                        {
                            continue;
                        }


                        string localizador = datos[0];
                        string destino = datos[1];
                        string origen = datos[2];
                        string nombrePasajero = datos[3];
                        string primerApellido = datos[4];
                        string segundoApellido = datos[5];
                        string documentoIdentidad = datos[6];

                        // Conversión de fecha con manejo de excepciones por fila
                        DateTime fechaReserva = DateTime.Parse(datos[7]);

                        ReservasDTO reserva = new ReservasDTO(
                            localizador,
                            destino,
                            origen,
                            nombrePasajero,
                            primerApellido,
                            segundoApellido,
                            documentoIdentidad,
                            fechaReserva
                        );

                        listaReservas.Add(reserva);
                    }
                    catch (FormatException)
                    {
                        // Captura errores si el Parse() de la fecha falla
                        Console.WriteLine($"[Aviso] Formato de fecha no válido en la línea {numeroLinea}. Se omite la fila.");
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // Captura errores por si acaso alguna variable intenta acceder a una posición que no existe
                        Console.WriteLine($"[Aviso] Faltan datos en la línea {numeroLinea}. Se omite la fila.");
                    }
                    catch (Exception ex) // Catch de seguridad genérico por fila
                    {
                        Console.WriteLine($"[Aviso] Error inesperado en la línea {numeroLinea}: {ex.Message}. Se omite la fila.");
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error al leer el archivo CSV: {ex.Message}");
        }

        return listaReservas;
    }
    #endregion

    #region Agrupar
    public static IEnumerable<IGrouping<(string Destino, DateTime Fecha), ReservasDTO>> AgruparReservas(List<ReservasDTO> reservas)
    {
        return reservas
            .GroupBy(r => (
                Destino: r.Destino,
                Fecha: r.FechaReserva.Date))
            .OrderBy(g => g.Key.Destino)
            .ThenBy(g => g.Key.Fecha);
    }
    #endregion

    #region Generar Nuevo Fichero
    public static void GenerarFicheros(string extension, IEnumerable<IGrouping<(string Destino, DateTime Fecha), ReservasDTO>> grupos)
    {

        string ext = extension.ToLower().Trim();

        foreach (var grupo in grupos)
        {
            string destino = grupo.Key.Destino;
            string fecha = grupo.Key.Fecha.ToString("yyyy_MM_dd");
            string nombreArchivo = $"AirTortilla_{destino}_{fecha}.{ext}";

            // Convertimos el grupo en una lista real para poder serializarla
            List<ReservasDTO> listaGrupo = grupo.ToList();

            try
            {
                switch (ext)
                {
                    case "csv":
                        var lineasCsv = new List<string>();

                        lineasCsv.Add(ReservasDTO.GetCsvHeader());
                        lineasCsv.AddRange(listaGrupo.Select(r => r.ToCsvLine()));

                        File.WriteAllLines(nombreArchivo, lineasCsv);
                        break;

                    case "txt":
                        var lineasTxt = listaGrupo.Select(r => r.ToTxtLine());

                        File.WriteAllLines(nombreArchivo, lineasTxt);
                        break;

                    case "json":
                        var opcionesJson = new JsonSerializerOptions { WriteIndented = true };
                        string jsonString = JsonSerializer.Serialize(listaGrupo, opcionesJson);

                        File.WriteAllText(nombreArchivo, jsonString);
                        break;

                    case "xml":
                        XmlSerializer serializer = new XmlSerializer(typeof(List<ReservasDTO>));
                        using (StreamWriter writer = new StreamWriter(nombreArchivo))
                        {
                            serializer.Serialize(writer, listaGrupo);
                        }
                        break;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error al escribir {nombreArchivo}: {ex.Message}");
            }
        }
        Console.WriteLine($"Archivos .{ext} generados con éxito bajo estructura estándar.");
    }
    #endregion

    #region Lectura de Golpe del Fichero Generado
    public static void ConsultarModalidadTotal(string ruta)
    {
        if (!File.Exists(ruta))
        { 
          Console.WriteLine("Archivo no encontrado."); 
          return; 
        }

        try
        {
            Console.WriteLine(File.ReadAllText(ruta));
        }
        catch (IOException ex) { Console.WriteLine($"Error de lectura: {ex.Message}"); }
    }
    #endregion

    #region Lectura por Linea del Fichero Generado
    public static void ConsultarModalidadLineaALinea(string ruta)
    {
        if (!File.Exists(ruta)) 
        { 
            Console.WriteLine("Archivo no encontrado.");
            return; 
        }

        try
        {
            using (StreamReader sr = new StreamReader(ruta))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    Console.WriteLine(linea);
                    Console.WriteLine(">> Presiona [ENTER] para la siguiente línea o escribe [Q] para salir...");

                    if (Console.ReadLine()?.Trim().ToUpper() == "Q") break;
                }
            }
            Console.WriteLine("Fin del archivo.");
        }
        catch (IOException ex) { Console.WriteLine($"Error de lectura: {ex.Message}"); }
    }
    #endregion

}


