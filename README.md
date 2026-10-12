
## Arquitectura del Sistema

A continuación se detalla el flujo de datos desde la lectura del CSV hasta la generación de los ficheros:

![Diagrama de Arquitectura](assets/AirTortilla_Diagrama.drawio.png)

## Estructura del Archivo CSV de Entrada

Para ejecutar correctamente el conversor, el archivo CSV de origen debe contener una primera línea de **cabecera** obligatoria, seguida de los datos. El delimitador debe ser una coma (`,`).

**Orden de las columnas (8 en total):**
`Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva`

*Nota sobre la fecha:* Debe ser compatible con el formato estándar `yyyy-MM-dd` (ej. 2026-05-14).

### Ejemplo de datos válidos (Casos de Coincidencia)
Según los requisitos del proyecto, el sistema gestiona reservas inequívocas mediante el `Localizador` cuando el Nombre, Origen y Destino coinciden. Para probar la validación de "al menos 5 personas coincidentes", el archivo CSV de prueba debe contener filas estructuradas de esta manera:

```csv
Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva
LOC001,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-14
LOC002,FR,ES,Juan,Perez,Gomez,22222222B,2026-05-14
LOC003,FR,ES,Juan,Perez,Gomez,33333333C,2026-05-14
LOC004,FR,ES,Juan,Perez,Gomez,44444444D,2026-05-14
LOC005,FR,ES,Juan,Perez,Gomez,55555555E,2026-05-14
