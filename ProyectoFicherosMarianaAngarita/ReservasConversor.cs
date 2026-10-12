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
    #region Codigo ISO
 
    private static readonly HashSet<string> CodigosIso = new HashSet<string>
    {
        "AD","AE","AF","AG","AI","AL","AM","AO","AQ","AR","AS","AT","AU","AW","AX","AZ",
        "BA","BB","BD","BE","BF","BG","BH","BI","BJ","BL","BM","BN","BO","BQ","BR","BS","BT","BV","BW","BY","BZ",
        "CA","CC","CD","CF","CG","CH","CI","CK","CL","CM","CN","CO","CR","CU","CV","CW","CX","CY","CZ",
        "DE","DJ","DK","DM","DO","DZ",
        "EC","EE","EG","EH","ER","ES","ET",
        "FI","FJ","FK","FM","FO","FR",
        "GA","GB","GD","GE","GF","GG","GH","GI","GL","GM","GN","GP","GQ","GR","GS","GT","GU","GW","GY",
        "HK","HM","HN","HR","HT","HU",
        "ID","IE","IL","IM","IN","IO","IQ","IR","IS","IT",
        "JE","JM","JO","JP",
        "KE","KG","KH","KI","KM","KN","KP","KR","KW","KY","KZ",
        "LA","LB","LC","LI","LK","LR","LS","LT","LU","LV","LY",
        "MA","MC","MD","ME","MF","MG","MH","MK","ML","MM","MN","MO","MP","MQ","MR","MS","MT","MU","MV","MW","MX","MY","MZ",
        "NA","NC","NE","NF","NG","NI","NL","NO","NP","NR","NU","NZ",
        "OM",
        "PA","PE","PF","PG","PH","PK","PL","PM","PN","PR","PS","PT","PW","PY",
        "QA",
        "RE","RO","RS","RU","RW",
        "SA","SB","SC","SD","SE","SG","SH","SI","SJ","SK","SL","SM","SN","SO","SR","SS","ST","SV","SX","SY","SZ",
        "TC","TD","TF","TG","TH","TJ","TK","TL","TM","TN","TO","TR","TT","TV","TW","TZ",
        "UA","UG","UM","US","UY","UZ",
        "VA","VC","VE","VG","VI","VN","VU",
        "WF","WS",
        "YE","YT",
        "ZA","ZM","ZW"
    };
    #endregion
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
        return CodigosIso.Contains(codigo);
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
                .ToList();

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


