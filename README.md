
## Arquitectura del Sistema

A continuación se detalla el flujo de datos desde la lectura del CSV hasta la generación de los ficheros:

![Diagrama de Arquitectura](assets/AirTortilla_Diagrama.drawio.png)

## Requisitos y ejecución

- .NET 10 SDK (comprobar con `dotnet --version`).
- Visual Studio 2022 o superior (opcional; también se puede ejecutar por terminal).

### Opción A: terminal (recomendada, funciona en cualquier PC)

Desde la carpeta del proyecto (la que contiene el `.csproj`):

    dotnet run

`dotnet run` restaura los paquetes y compila automáticamente antes de ejecutar.

### Opción B: Visual Studio

Al clonar el repositorio en un equipo nuevo, la carpeta `obj/` no existe
(no se sube al repositorio). Si al compilar aparece el error
`No se encuentra el archivo de recursos ...\obj\project.assets.json`:

1. En el Explorador de soluciones, clic derecho sobre la solución → **Restaurar paquetes NuGet**.
2. **Compilar → Recompilar solución**.

Para que no vuelva a ocurrir, activar en
**Herramientas → Opciones → Administrador de paquetes NuGet → General**
las dos opciones de restauración automática de paquetes.

### Ubicación de los ficheros generados

Los ficheros se crean en el directorio de ejecución, que el programa muestra al terminar
la generación (con Visual Studio: `bin/Debug/net10.0/`; con `dotnet run`: la carpeta
desde la que se lanza el comando). Para consultarlos con las opciones 2 y 3 basta con
indicar el nombre del fichero, no la ruta completa.

## Formato del CSV de entrada

- Codificación: UTF-8.
- La primera línea es una cabecera y se ignora.
- Separador: coma (`,`). Los campos no pueden contener comas.
- Columnas (en este orden):

| Columna | Descripción | Ejemplo |
|---|---|---|
| Localizador | Código único de reserva. Varios pasajeros pueden compartirlo | AT1001 |
| Destino | Código ISO 3166-1 alfa-2 del país | FR |
| Origen | Código ISO 3166-1 alfa-2 del país | ES |
| NombrePasajero | Nombre | Maria |
| PrimerApellido | Primer apellido | Garcia |
| SegundoApellido | Segundo apellido | Lopez |
| DocumentoIdentidad | Identificador único del pasajero | 10000001A |
| FechaReserva | Formato `yyyy-MM-dd` | 2026-05-14 |

- Ejemplo:

```csv
Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva
LOC001,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-14
LOC002,FR,ES,Juan,Perez,Gomez,22222222B,2026-05-14
LOC003,FR,ES,Juan,Perez,Gomez,33333333C,2026-05-14
LOC004,FR,ES,Juan,Perez,Gomez,44444444D,2026-05-14
LOC005,FR,ES,Juan,Perez,Gomez,55555555E,2026-05-14
```

### Restricciones

- **Ningún campo puede contener comas.** El programa separa los campos con `Split(',')`
  y no admite comillas, así que una coma dentro de un campo desplazaría las columnas.
  Las filas que no tengan exactamente 8 campos se omiten con un aviso.
- Los códigos ISO de Destino y Origen se validan contra la lista oficial ISO 3166-1 alfa-2
  (249 códigos), sin distinguir mayúsculas y minúsculas. Códigos no oficiales como `UK`
  o `XK` se rechazan (Reino Unido es `GB`).
- Las filas vacías se ignoran. Las filas con campos de más o de menos, código ISO no válido
  o fecha no válida se omiten mostrando un aviso con el número de línea.

## Ficheros generados

Se genera un fichero por cada combinación de país de destino y fecha de reserva,
con el formato `AirTortilla_XX_YYYY_MM_DD.<ext>`.

### Formatos de salida

El usuario elige el formato en cada ejecución (solo se genera uno cada vez):

| Opción | Formato | Contenido |
|---|---|---|
| 1 | `.csv` | Cabecera + una línea por reserva, separada por comas |
| 2 | `.json` | Lista de objetos con sangría (`JsonSerializer`) |
| 3 | `.xml` | Lista de objetos (`XmlSerializer`) |
| 4 | `.txt` | Una línea por reserva, campos separados por ` \| `, sin cabecera |

En CSV y TXT la fecha se escribe como `yyyy-MM-dd`. En JSON y XML se serializa el
`DateTime` completo (`2026-05-14T00:00:00`), ya que ese tipo siempre incluye la hora.

### Ordenación

Antes de escribir los ficheros se ordenan los datos en dos niveles:

1. **Grupos (orden de generación):** por país de destino y, después, por fecha.
2. **Reservas dentro de cada fichero:** por `Localizador` y, dentro del mismo
   localizador, por `DocumentoIdentidad`. Así los pasajeros de una misma reserva
   aparecen juntos y en un orden determinista, sea cual sea el orden del CSV de entrada.

### Complejidad (notación Big O)

Siendo *n* el número de filas del CSV y *g* el número de ficheros generados:

| Fase | Operación | Complejidad |
|---|---|---|
| Lectura | Recorrer las *n* líneas | O(n) |
| Validación ISO | Búsqueda en `HashSet` de 249 códigos | O(1) por fila → O(n) |
| Agrupación | `GroupBy` (basado en tabla hash) | O(n) |
| Orden de grupos | `OrderBy` + `ThenBy` sobre *g* grupos | O(g log g) |
| Orden dentro de cada grupo | `OrderBy` + `ThenBy` sobre cada grupo | O(n log n) en el peor caso |
| Escritura | Escribir las *n* reservas | O(n) |

**Total: O(n log n)**, determinado por la ordenación. La memoria usada es O(n),
porque todas las reservas se cargan en una lista antes de agruparlas.

Decisiones relacionadas con el rendimiento:
- Los códigos ISO se guardan en un `HashSet` en lugar de una `List`. Comprobar si un
  código existe cuesta O(1) de media, frente a O(249) recorriendo una lista.
- `ThenBy` no hace una segunda ordenación: añade un criterio de desempate a la misma
  comparación, así que no cambia la complejidad.
- `OrderBy` de LINQ es una ordenación **estable**: si dos reservas empatan en todos
  los criterios, conservan el orden en que venían en el CSV.
  
## Conclusión

El sistema cumple los requisitos del enunciado: lee un fichero CSV de reservas, divide las reservas en ficheros por país de
destino y fecha (añadido) con el formato `AirTortilla_XX_YYYY_MM_DD`, y permite consultarlos
en modalidad total y línea a línea desde un menú de terminal.

Como ampliaciones sobre el enunciado, se han añadido:

- Exportación en cuatro formatos: CSV, JSON, XML y TXT.
- Ordenación de los ficheros y de las reservas que contienen, con una complejidad
  total de O(n log n).
- Validación de cada fila (número de campos, código ISO oficial y formato de fecha)
  con avisos que indican la línea y el motivo, de modo que una fila incorrecta no
  detiene el procesamiento del resto.
- Resumen de la generación (ficheros correctos y fallidos) y ubicación de salida.

### Limitaciones conocidas

- Los campos no pueden contener comas, porque la lectura no interpreta comillas.
- Todas las reservas se cargan en memoria antes de agruparlas (O(n)), lo que es
  adecuado para el volumen de una práctica pero no para ficheros muy grandes.
- La lista de códigos ISO está escrita en el código y habría que actualizarla a mano
  si la norma cambia.
- Si se vuelve a generar, los ficheros con el mismo nombre se sobrescriben y los de
  ejecuciones anteriores no se eliminan.

### Posibles mejoras

- Usar una librería de lectura de CSV (por ejemplo, CsvHelper) para admitir campos
  entre comillas.
- Permitir elegir la carpeta de salida.
- Añadir pruebas unitarias automáticas para la validación, la agrupación y la ordenación.
