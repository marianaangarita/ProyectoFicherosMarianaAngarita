using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using ProyectoReservas.DTO;
using System.Text.Json;
using System.Xml.Serialization;


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
                    numeroLinea++; 

                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    try
                    {
                        string[] datos = linea.Split(',');

                        if (datos.Length != 8)
                        {
                            Console.WriteLine($"[Aviso] Número de campos incorrecto en la línea {numeroLinea} (se esperaban 8 y hay {datos.Length}). Se omite la fila.");
                            continue;
                        }
                        string destino = datos[1].Trim().ToUpper();
                        string origen = datos[2].Trim().ToUpper();

                        if (!EsCodigoIso(destino) || !EsCodigoIso(origen))
                        {
                            Console.WriteLine($"[Aviso] Código ISO no válido en la línea {numeroLinea} (Destino: '{datos[1]}', Origen: '{datos[2]}'). Se omite la fila.");
                            continue;
                        }
                        DateTime fechaReserva = DateTime.Parse(datos[7]);


                        ReservasDTO reserva = new ReservasDTO(
                            datos[0].Trim(),
                            destino,
                            origen,
                            datos[3].Trim(),
                            datos[4].Trim(),
                            datos[5].Trim(),
                            datos[6].Trim(),
                            fechaReserva
                        );

                        listaReservas.Add(reserva);
                    }
                    catch (FormatException)
                    {
                        // Captura errores si el Parse() de la fecha falla
                        Console.WriteLine($"[Aviso] Formato de fecha no válido en la línea {numeroLinea}. Se omite la fila.");
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

    private static bool EsCodigoIso(string codigo)
    {
        return codigo.Length == 2 && codigo.All(c => c >= 'A' && c <= 'Z');
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
        int generados = 0;
        int fallidos = 0;

        foreach (var grupo in grupos)
        {
            string destino = grupo.Key.Destino;
            string fecha = grupo.Key.Fecha.ToString("yyyy_MM_dd");
            string nombreArchivo = $"AirTortilla_{destino}_{fecha}.{ext}";

            // Ordenar las reservas del fichero por localizador (y por DNI dentro del mismo localizador)
            List<ReservasDTO> listaGrupo = grupo
                .OrderBy(r => r.Localizador)
                .ThenBy(r => r.DocumentoIdentidad)
                .ToList()

            try
            {
                switch (ext)
                {
                    case "csv":
                        var lineasCsv = new List<string> { ReservasDTO.GetCsvHeader() };
                        lineasCsv.AddRange(listaGrupo.Select(r => r.ToCsvLine()));
                        File.WriteAllLines(nombreArchivo, lineasCsv);

                        break;

                    case "txt":
                        File.WriteAllLines(nombreArchivo, listaGrupo.Select(r => r.ToTxtLine()));
                        break;

                    case "json":
                        var opcionesJson = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(nombreArchivo, JsonSerializer.Serialize(listaGrupo, opcionesJson));
                        break;

                    case "xml":
                        XmlSerializer serializer = new XmlSerializer(typeof(List<ReservasDTO>));
                        using (StreamWriter writer = new StreamWriter(nombreArchivo))
                        {
                            serializer.Serialize(writer, listaGrupo);
                        }
                        break;
                }
                generados++;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                fallidos++;
                Console.WriteLine($"Error al escribir {nombreArchivo}: {ex.Message}");
            }
        }
        if (fallidos == 0)
            Console.WriteLine($"{generados} archivos .{ext} generados correctamente.");
        else
            Console.WriteLine($"{generados} archivos .{ext} generados, {fallidos} con error.");
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


