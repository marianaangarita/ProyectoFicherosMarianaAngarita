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

            while (!salir)
            {
                Console.Clear();
                Console.WriteLine("=========================================");
                Console.WriteLine("      SISTEMA DE RESERVAS AIRTORTILLA    ");
                Console.WriteLine("=========================================");
                Console.WriteLine("1. Generar ficheros de Reserva por país (Proceso automático)");
                Console.WriteLine("2. Consultar fichero generado (Modalidad TOTAL)");
                Console.WriteLine("3. Consultar fichero generado (Modalidad LÍNEA A LÍNEA)");
                Console.WriteLine("4. Salir");
                Console.WriteLine("=========================================");
                Console.Write("Elige una opción (1-4): ");

                string opcion = Console.ReadLine();

                int opcionInt = int.Parse(opcion);

                switch (opcionInt)
                {
                    case 1:
                        Console.WriteLine("\n[Generando archivos por país]");
                        // Lógica para leer el CSV principal y repartirlo en los AirTortilla_XX...
                        Console.WriteLine("Archivos generados con éxito. Presiona una tecla para volver.");
                        Console.ReadKey();
                        break;
                    case 2:
                        Console.WriteLine("\n[Consulta Total]");
                        Console.Write("Introduce la ruta o nombre del archivo a consultar: ");
                        string rutaTotal = Console.ReadLine();
                        // Lógica para File.ReadAllLines() y hacer un foreach imprimiendo todo
                        Console.WriteLine("\nFin del archivo. Presiona una tecla para volver.");
                        Console.ReadKey();
                        break;
                    case 3:
                        Console.WriteLine("\n[Consulta Línea a Línea]");
                        Console.Write("Introduce la ruta o nombre del archivo a consultar: ");
                        string rutaParcial = Console.ReadLine();
                        // Lógica con StreamReader, imprimiendo una línea y haciendo Console.ReadLine() para avanzar
                        Console.WriteLine("\nFin del archivo. Presiona una tecla para volver.");
                        Console.ReadKey();
                        break;
                    case 4:
                        salir = true;
                        Console.WriteLine("\nCerrando el sistema. ¡Hasta luego!");
                        break;
                    default:
                        Console.WriteLine("\nOpción no válida. Inténtalo de nuevo.");
                        Console.ReadKey();
                        break;

                }
            }

        }

    }

}

  

