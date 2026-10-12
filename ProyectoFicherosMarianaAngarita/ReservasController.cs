using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProyectoReservas.DTO;
using ProyectoReservas.Conversor;

namespace ProyectoReservas.Controlller
{
    public class ReservasController
    {
        static void Main(string[] args) //Falta añadir try-catch
        {
            bool salir = false;

            // Ruta del archivo base con los datos falsos (asegúrate de incluir los casos de duplicidad exigidos)
            string rutaBase = "reservas_airtortilla.csv";

            while (!salir)
            {
                Console.Clear();
                Console.WriteLine("=========================================");
                Console.WriteLine("     SISTEMA DE RESERVAS AIRTORTILLA     ");
                Console.WriteLine("=========================================");
                Console.WriteLine("1. Generar ficheros por país (CSV, JSON, XML, TXT)");
                Console.WriteLine("2. Consultar fichero (Modalidad TOTAL)");
                Console.WriteLine("3. Consultar fichero (Modalidad LÍNEA A LÍNEA)");
                Console.WriteLine("4. Salir");
                Console.WriteLine("=========================================");
                Console.Write("Elige una opción (1-4): ");

                string opcion = Console.ReadLine();
                int opcionInt = int.Parse(opcion);

                switch (opcionInt)
                {
                    //case 1:
                    //    Console.Write("¿En qué formato deseas exportar? (csv: 1 / json: 2 / xml: 3 / txt: 4): ");
                    //    string formato = Console.ReadLine();
                    //    var datos = ReservasConversor.LeerReservasDesdeCsv(rutaBase);
                    //    ReservasConversor.GenerarFicheros(formato, datos);
                    //    Pausar();
                    //    break;

                    case 2:
                        Console.Write("Introduce el nombre completo del archivo a consultar (ej. AirTortilla_FR_2026_05_14.csv): ");
                        ReservasConversor.ConsultarModalidadTotal(Console.ReadLine());
                        Pausar();
                        break;

                    case 3:
                        Console.Write("Introduce el nombre del archivo (Recomendado para CSV/TXT): ");
                        ReservasConversor.ConsultarModalidadLineaALinea(Console.ReadLine());
                        Pausar();
                        break;

                    case 4:
                        salir = true;
                        break;

                    default:
                        Console.WriteLine("\nOpción no válida. Inténtalo de nuevo.");
                        Console.ReadKey();
                        break;
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

  

