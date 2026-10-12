using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProyectoReservas.DTO;
using ProyectoReservas.Conversor;

namespace ProyectoReservas.Controller
{
    public class ReservasController
    {
        static void Main(string[] args) 
        {
            bool salir = false;

            while (!salir)
            {
                Console.Clear();
                Console.WriteLine("=========================================");
                Console.WriteLine("     SISTEMA DE RESERVAS AIRTORTILLA     ");
                Console.WriteLine("=========================================");
                Console.WriteLine("1. Generar ficheros por país de destino");
                Console.WriteLine("2. Consultar fichero (Modalidad TOTAL)");
                Console.WriteLine("3. Consultar fichero (Modalidad LÍNEA A LÍNEA)");
                Console.WriteLine("4. Salir");
                Console.WriteLine("=========================================");
                Console.Write("Elige una opción (1-4): ");

                try
                {
                    string opcion = Console.ReadLine();

                    switch (opcion)
                    {
                        case "1":
                            Console.Write("Introduce la ruta completa o el nombre del archivo .csv de origen: ");
                            string rutaBase = Console.ReadLine();

                            Console.Write("¿En qué formato deseas exportar? (1: csv / 2: json / 3: xml / 4: txt): ");
                            string opcionFormato = Console.ReadLine();
                            string formato = string.Empty;

                            switch (opcionFormato)
                            {
                                case "1": formato = "csv"; break;
                                case "2": formato = "json"; break;
                                case "3": formato = "xml"; break;
                                case "4": formato = "txt"; break;
                                default:
                                    Console.WriteLine("Formato no válido. Exportación cancelada.");
                                    Pausar();
                                    continue;
                            }

                            // 1. Leer todo el CSV 
                            var datos = ReservasConversor.LeerReservasDesdeCsv(rutaBase);

                            // Si la lista está vacía (archivo no existe o error), cancelamos la generación
                            if (datos.Count == 0)
                            {
                                Console.WriteLine("No se han cargado datos. Comprueba la ruta y el contenido del archivo.");
                                Pausar();
                                break;
                            }

                            // 2. Agrupar 
                            var datosAgrupados = ReservasConversor.AgruparReservas(datos);

                            // 3. Generar todos los ficheros de golpe
                            ReservasConversor.GenerarFicheros(formato, datosAgrupados);

                            Pausar();
                            break;

                        case "2":
                            Console.Write("Introduce el nombre completo del archivo a consultar (ej. AirTortilla_FR_2026_05_14.csv): ");
                            ReservasConversor.ConsultarModalidadTotal(Console.ReadLine());
                            Pausar();
                            break;

                        case "3":
                            Console.Write("Introduce el nombre del archivo (Recomendado para CSV/TXT): ");
                            ReservasConversor.ConsultarModalidadLineaALinea(Console.ReadLine());
                            Pausar();
                            break;

                        case "4":
                            salir = true;
                            break;

                        default:
                            Console.WriteLine("\nOpción no válida. Inténtalo de nuevo.");
                            Console.ReadKey();
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("\nError: Por favor, introduce un número válido.");
                    Pausar();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nOcurrió un error inesperado: {ex.Message}");
                    Pausar();
                }
            }
        }

        static void Pausar()
        {
            Console.WriteLine("\nPresiona cualquier tecla para continuar...");
            Console.ReadKey();
        }


    }

}

  

