# AirTortilla · Gestión de reservas

Práctica de **Acceso a Datos (DAM)**. Sistema de gestión de reservas de la aerolínea AirTortilla.

- **Entrega 1:** aplicación de consola en C# que leía un CSV de reservas y lo dividía en ficheros por país de destino y fecha.
- **Entrega 2 (actual):** migración a **aplicación web (Blazor)** con **base de datos relacional (MySQL)**, inicio de sesión, gestión de usuarios por rol y trazabilidad de las modificaciones.

> El proyecto de consola de la entrega 1 se conserva en la solución (`ProyectoFicherosMarianaAngarita/`) como referencia de la migración. El proyecto activo es `AirTortilla.Web/`.

---

## Índice

1. [Objetivos de la entrega 2](#objetivos-de-la-entrega-2)
2. [Migración de tecnología](#migración-de-tecnología)
3. [Arquitectura del sistema](#arquitectura-del-sistema)
4. [Modelo de datos](#modelo-de-datos)
5. [User Stories y tareas medibles](#user-stories-y-tareas-medibles)
6. [Seguridad](#seguridad)
7. [Patrones de diseño aplicados](#patrones-de-diseño-aplicados)
8. [Requisitos y ejecución](#requisitos-y-ejecución)
9. [Formato del CSV de entrada](#formato-del-csv-de-entrada)
10. [Ficheros exportados](#ficheros-exportados)
11. [Rendimiento y complejidad](#rendimiento-y-complejidad)
12. [Conclusión, limitaciones y mejoras](#conclusión)

---

## Objetivos de la entrega 2

User Stories de la iteración:

| ID | User Story |
|---|---|
| **US1** | Como **Empleado** quiero poder hacer *log in* para acceder a los recursos de la compañía. |
| **US2** | Como **Administrador de Sistemas** quiero poder dar de alta usuarios para garantizar la seguridad interna de la aplicación. |
| **US3** | Como **Administrador de Sistemas** quiero mantener la trazabilidad de las modificaciones para mantener las buenas prácticas de seguridad en la aplicación. |

Además, por requisito legal, toda la infraestructura de datos se migra a una **base de datos relacional**. Los contenedores (Docker) se consideran no críticos para esta entrega.

---

## Migración de tecnología

> El enunciado indica que esta es la última versión que permite migrar el stack principal sin coste. Se documenta aquí el momento y los cambios.

### Stack: antes y después

| Capa | Entrega 1 | Entrega 2 | Motivo del cambio |
|---|---|---|---|
| Lenguaje / backend | C# (.NET 10) | C# (.NET 10) | Se mantiene. |
| Interfaz | Terminal (menú de consola) | **Web: Blazor Server** (ASP.NET Core) | Login y gestión de usuarios requieren una interfaz multiusuario. Blazor permite seguir en C# sin JavaScript. |
| Persistencia | Ficheros (CSV/JSON/XML/TXT) | **MySQL 8** | Requisito legal de base de datos relacional. |
| Acceso a datos | `StreamReader` / `File.*` | **Entity Framework Core 10** + `MySql.EntityFrameworkCore` (proveedor oficial de Oracle) | ORM: consultas en C# traducidas a SQL, menos código repetitivo. |
| Seguridad | — | Cookie de autenticación, roles, **BCrypt** | Nuevas User Stories US1 y US2. |

> **Nota técnica:** no se usa el proveedor Pomelo (el más habitual en tutoriales) porque, a fecha de esta entrega, no tiene versión compatible con EF Core 10.

### Equivalencia de funciones: consola → web

| Entrega 1 (consola) | Entrega 2 (web) |
|---|---|
| Opción 1: introducir ruta del CSV | Página **Importar CSV**: se sube el fichero desde el navegador |
| Opción 1: generar **todos** los ficheros en disco | Los datos se guardan en MySQL; cada fichero se genera **bajo demanda** desde **Reservas → Exportar** y se descarga |
| Opción 2: consulta TOTAL (`File.ReadAllText`) | Botón **Total**: tabla con todas las filas del grupo |
| Opción 3: LÍNEA A LÍNEA (`[ENTER]` / `[Q]`) | Botón **Línea a línea**: "Siguiente línea" / "Salir" |
| Escribir a mano el nombre del fichero | Elegirlo de la lista de grupos destino + fecha |
| `HashSet` con 249 códigos ISO en el código | Tabla `Pais` en la base de datos con clave foránea |
| Avisos por consola | Avisos en pantalla con número de línea y motivo |

**Idea clave:** en la entrega 1, cada combinación destino + fecha era un **fichero**. En la entrega 2 es una **consulta** a la base de datos (`ObtenerGruposAsync`), y el fichero `AirTortilla_XX_YYYY_MM_DD` solo se genera cuando alguien lo exporta.

### Código reutilizado

- `ReservasDTO` (con `GetCsvHeader`, `ToCsvLine`, `ToTxtLine`) se reutiliza sin cambios de lógica para transportar los datos y generar los ficheros exportados.
- La lógica de lectura y validación del CSV se adapta a `LectorCsvReservas` (lee de un `Stream` subido en lugar de una ruta; avisos en lista en lugar de `Console.WriteLine`).
- La lógica de los cuatro formatos de `GenerarFicheros` se reparte en cuatro clases exportadoras (patrón Factory).

---

## Arquitectura del sistema

![Diagrama de Arquitectura - Entrega 2](assets/AirTortilla_Arquitectura_E2_V3.drawio.png)

Estructura del proyecto web:

```
AirTortilla.Web/
├── Components/
│   ├── Layout/            MainLayout, NavMenu (menú según rol)
│   └── Pages/             Login, Home, Importar, Reservas, Usuarios, Auditoria, AccesoDenegado
├── Data/
│   ├── AirTortillaContext.cs   Contexto de EF Core
│   ├── Auditor.cs              Registro de auditoría
│   ├── Entities/               Entidades (generadas por scaffolding desde MySQL)
│   ├── DAO/                    IReservaDao + ReservaDaoMySql (patrón DAO)
│   ├── CSV/                    LectorCsvReservas
│   └── Exportacion/            IExportador, exportadores y ExportadorFactory
├── DTO/                   ReservasDTO
└── Program.cs             Configuración: BD, autenticación, rutas /logout y /exportar
database/
├── airtortilla_schema.sql Script de creación de la base de datos
└── AirTortilla_ER.drawio  Diagrama entidad-relación (editable)
```

---

## Modelo de datos

![Diagrama Entidad-Relación](assets/AirTortilla_ER.drawio.png)

El script completo está en [`database/airtortilla_schema.sql`](database/airtortilla_schema.sql).

### Tablas

| Bloque | Tabla | Contenido |
|---|---|---|
| Reservas | `Pais` | Catálogo ISO 3166-1 alfa-2 (249 códigos) |
| | `Reserva` | Una fila por localizador: origen, destino y fecha |
| | `Pasajero` | Una fila por documento de identidad |
| | `ReservaPasajero` | Tabla intermedia de la relación N:M |
| Seguridad | `Rol` | `Administrador`, `Empleado` |
| | `Usuario` | Usuario, hash de contraseña, rol, activo, quién lo dio de alta |
| Trazabilidad | `Auditoria` | Quién, cuándo, qué acción, sobre qué registro, valores antes/después (JSON) |

### Decisiones de diseño

- **Normalización.** La fila plana del CSV se divide en `Reserva`, `Pasajero` y `ReservaPasajero`. Ejemplo: `LOC001` con 5 pasajeros pasa de 5 filas que repiten destino, origen y fecha a **1 reserva + 5 pasajeros + 5 enlaces**.
- **Relación N:M Reserva–Pasajero.** Una reserva incluye varios pasajeros y un pasajero puede figurar en varias reservas. Como una base de datos relacional no puede representar N:M directamente, se resuelve con `ReservaPasajero` (clave primaria compuesta por las dos claves foráneas). Un pasajero que aparece en dos reservas se guarda **una sola vez**.
- **Origen, destino y fecha pertenecen a la reserva**, no al pasajero: todos los pasajeros de un localizador viajan juntos. Si en el CSV un mismo localizador tiene destinos o fechas distintos, se rechaza.
- **`Usuario.Activo`** permite dar de baja sin borrar, para que la auditoría nunca apunte a un usuario inexistente.
- **`Usuario.CreadoPorId`** (relación de la tabla consigo misma) registra qué administrador dio de alta a cada usuario.
- **`Auditoria.UsuarioId` admite NULL** únicamente para registrar intentos de acceso con un nombre de usuario que no existe.

---

## User Stories y tareas medibles

Cada User Story se descompone en tareas con un **criterio de aceptación verificable** (se comprueba con una acción concreta y un resultado observable).

### US1 · Inicio de sesión

| # | Tarea | Criterio de aceptación | Estado |
|---|---|---|---|
| 1.1 | Crear tablas `Usuario` y `Rol` | Existen en MySQL con `NombreUsuario` único y FK a `Rol` | ✅ |
| 1.2 | Guardar contraseñas con hash BCrypt | En `Usuario.PasswordHash` el valor empieza por `$2a$11$`; la contraseña no aparece en la BD | ✅ |
| 1.3 | Crear un administrador inicial al arrancar si no hay usuarios | Con la tabla vacía, al arrancar existe `admin` con rol Administrador; su contraseña se lee de *user-secrets*, no del código | ✅ |
| 1.4 | Página de login | Credenciales correctas → página de inicio con "Hola, *usuario*"; incorrectas → "Usuario o contraseña incorrectos" | ✅ |
| 1.5 | Mismo mensaje para cualquier fallo | Usuario inexistente, desactivado o contraseña errónea muestran **el mismo** mensaje | ✅ |
| 1.6 | Proteger todas las páginas por defecto | Sin sesión, `/reservas` redirige a `/login?ReturnUrl=%2Freservas`; tras iniciar sesión se vuelve a `/reservas` | ✅ |
| 1.7 | Cerrar sesión | El botón "Cerrar sesión" lleva al login y las páginas protegidas vuelven a redirigir | ✅ |
| 1.8 | Registrar accesos | Cada login correcto genera una fila `LOGIN` y cada fallo una fila `LOGIN_FALLIDO` (sin guardar la contraseña escrita) | ✅ |

### US2 · Alta de usuarios por el administrador

| # | Tarea | Criterio de aceptación | Estado |
|---|---|---|---|
| 2.1 | Página de gestión de usuarios | Un Administrador ve `/usuarios` con formulario de alta y lista de usuarios | ✅ |
| 2.2 | Restringir al rol Administrador | Un Empleado no ve el enlace en el menú y al escribir `/usuarios` va a "Acceso denegado" | ✅ |
| 2.3 | Validar el alta | Nombre de 3–50 caracteres, contraseña de mínimo 8, rol obligatorio; nombre duplicado → "Ya existe un usuario llamado…" | ✅ |
| 2.4 | Registrar quién da de alta | La columna "Creado por" muestra el administrador; en BD se guarda `CreadoPorId` | ✅ |
| 2.5 | Alta y auditoría atómicas | Usuario y registro de auditoría se guardan en una única transacción | ✅ |
| 2.6 | Activar / desactivar usuarios | Un usuario desactivado no puede iniciar sesión; un administrador no puede desactivarse a sí mismo | ✅ |
| 2.7 | Expulsar sesiones abiertas al desactivar | Con `empleado1` dentro en otro navegador, al desactivarlo y navegar o recargar, se le redirige al login | ✅ |

### US3 · Trazabilidad de modificaciones

| # | Tarea | Criterio de aceptación | Estado |
|---|---|---|---|
| 3.1 | Tabla `Auditoria` | Guarda usuario, fecha, acción, tabla, id del registro y valores antes/después en JSON | ✅ |
| 3.2 | Componente reutilizable `Auditor` | Toda operación que modifica datos llama a `Auditor.Registrar` dentro del mismo `SaveChanges` | ✅ |
| 3.3 | Auditar altas | Alta de usuario y cada reserva importada generan una fila `INSERT` | ✅ |
| 3.4 | Auditar modificaciones con antes/después | Desactivar un usuario genera `UPDATE` con `{"Activo": true}` → `{"Activo": false}` | ✅ |
| 3.5 | Auditar importaciones y exportaciones | Cada importación genera `IMPORTAR` con el resumen; cada descarga genera `EXPORTAR` con el nombre del fichero | ✅ |
| 3.6 | Página de auditoría (solo Administrador) | `/auditoria` muestra los 200 registros más recientes, filtrables por acción, con etiquetas legibles ("Modificación de usuario") | ✅ |
| 3.7 | Auditoría de solo lectura en la aplicación | No existe ninguna opción para editar o borrar registros de auditoría | ✅ |

### Migración a base de datos relacional

| # | Tarea | Criterio de aceptación | Estado |
|---|---|---|---|
| M.1 | Esquema y diagrama ER | `database/airtortilla_schema.sql` crea las 7 tablas; diagrama en `assets/` | ✅ |
| M.2 | Importar CSV a MySQL | El CSV de ejemplo importa las reservas válidas y muestra un aviso por cada fila rechazada | ✅ |
| M.3 | Importación idempotente | Importar dos veces el mismo CSV no duplica reservas ni pasajeros | ✅ |
| M.4 | Consulta TOTAL y LÍNEA A LÍNEA | Mismo comportamiento que las opciones 2 y 3 de la consola, incluidos "Fin del fichero" y "Lectura interrumpida por el usuario" | ✅ |
| M.5 | Exportar en 4 formatos | Se descarga `AirTortilla_XX_YYYY_MM_DD.<ext>` con el mismo contenido que generaba la entrega 1 | ✅ |
| M.6 | Contenedores (Docker) | — | ⏳ No crítico, pendiente |

---

## Seguridad

| Medida | Qué protege |
|---|---|
| **BCrypt** (coste 11) | Las contraseñas no se guardan; solo su hash. Se verifica calculando el hash de lo escrito. |
| **Mensaje de error único** en el login | Impide averiguar qué nombres de usuario existen. |
| **Cookie `HttpOnly`**, caducidad de 8 h renovable | El JavaScript del navegador no puede leer la cookie (mitiga robos por XSS). |
| **Revalidación de la sesión en cada petición** (`OnValidatePrincipal`) | Si un administrador desactiva a un usuario que tiene la sesión abierta, en su siguiente petición se borra su cookie y se le envía al login. |
| **`[Authorize]` por defecto** en todas las páginas, `[AllowAnonymous]` solo en Login, Error y NotFound | Una página nueva queda protegida aunque se olvide indicarlo. |
| **Roles** (`[Authorize(Roles = "Administrador")]`) | Usuarios y Auditoría solo para administradores; ocultar el enlace es solo visual, la protección real está en la página. |
| **`/exportar` con `.RequireAuthorization()`** | La descarga de datos personales exige sesión iniciada. |
| **Antiforgery token** en formularios y logout por `POST` | Protección frente a CSRF (otra web no puede cerrar la sesión ni enviar formularios en tu nombre). |
| **Validación de `ReturnUrl`** | Solo se redirige a rutas internas (evita redirecciones a webs externas tras el login). |
| **User-secrets** | La cadena de conexión y la contraseña del admin inicial no están en el repositorio. |
| **Usuario de MySQL propio** (`airtortilla_app`) | La aplicación no usa `root`; solo tiene `SELECT, INSERT, UPDATE, DELETE` sobre `airtortilla`. |
| **Nunca se audita la contraseña escrita** en un login fallido | Un error tipográfico de la contraseña real no queda registrado. |

---

## Patrones de diseño aplicados

### DTO (Data Transfer Object)
`ReservasDTO` es la fila plana del CSV: el formato en que los datos **viajan** (entran del CSV, salen a los ficheros exportados). Las entidades (`Reserva`, `Pasajero`…) son el formato en que los datos **viven** en MySQL.

### DAO (Data Access Object)
`IReservaDao` define qué se puede hacer con las reservas (importar, listar grupos, obtener por destino y fecha); `ReservaDaoMySql` lo implementa con EF Core y traduce entre DTO y entidades. Las páginas solo conocen la interfaz; la implementación se elige en una línea de `Program.cs`:

```csharp
builder.Services.AddScoped<IReservaDao, ReservaDaoMySql>();
```

Para volver a trabajar con ficheros bastaría con crear `ReservaDaoFicheros` y cambiar esa línea.

### Factory
En la entrega 1, `GenerarFicheros` tenía un `switch` que decidía el formato **y** contenía la lógica de escritura de los cuatro. Ahora cada formato es una clase (`ExportadorCsv`, `ExportadorJson`, `ExportadorXml`, `ExportadorTxt`) que implementa `IExportador`, y `ExportadorFactory.Crear("json")` devuelve la adecuada. Añadir un formato nuevo = una clase + una línea en la fábrica.

---

## Requisitos y ejecución

### Requisitos

- .NET 10 SDK (`dotnet --version`)
- MySQL Server 8 y MySQL Workbench
- Visual Studio 2022 o superior (opcional)

### Paso 1 · Crear la base de datos

En MySQL Workbench, abrir y ejecutar `database/airtortilla_schema.sql` (botón ⚡ "Execute"). Crea la base de datos `airtortilla`, las tablas, los roles y los 249 países.

> ⚠️ El script empieza con `DROP DATABASE IF EXISTS airtortilla`: si se vuelve a ejecutar, **borra todos los datos**.

### Paso 2 · Crear el usuario de la aplicación

En Workbench (elegir una contraseña propia):

```sql
CREATE USER 'airtortilla_app'@'localhost' IDENTIFIED BY '<contraseña>';
GRANT SELECT, INSERT, UPDATE, DELETE ON airtortilla.* TO 'airtortilla_app'@'localhost';
FLUSH PRIVILEGES;
```

### Paso 3 · Configurar los secretos

Desde la carpeta `AirTortilla.Web/`:

```
dotnet user-secrets set "ConnectionStrings:AirTortilla" "server=localhost;port=3306;database=airtortilla;user=airtortilla_app;password=<contraseña>"
dotnet user-secrets set "AdminInicial:Password" "<contraseña del administrador>"
```

Los *user-secrets* se guardan en el perfil de Windows del usuario, fuera del repositorio.

### Paso 4 · Ejecutar

```
cd AirTortilla.Web
dotnet run
```

Abrir la dirección que aparece en la terminal (`http://localhost:5223`). En el primer arranque se crea automáticamente el usuario **`admin`** con la contraseña del paso 3.

Si falta algún secreto, la aplicación se detiene con un mensaje que indica cuál.

### Visual Studio

Si al compilar aparece `No se encuentra el archivo de recursos ...\obj\project.assets.json`: clic derecho sobre la solución → **Restaurar paquetes NuGet** → **Compilar → Recompilar solución**. Si el servidor está en marcha desde la terminal, pararlo (`Ctrl+C`) antes de compilar desde Visual Studio, porque bloquea el ejecutable.

---

## Formato del CSV de entrada

Se mantiene el formato de la entrega 1. Los CSV de prueba no se incluyen en el repositorio: con esta especificación se puede crear uno propio.

- Codificación: UTF-8.
- La primera línea es una cabecera y se ignora.
- Separador: coma (`,`). Los campos no pueden contener comas.
- Columnas (en este orden):

| Columna | Descripción | Restricción | Ejemplo |
|---|---|---|---|
| Localizador | Código de reserva. Varios pasajeros pueden compartirlo | Obligatorio, máx. 20 | AT1001 |
| Destino | Código ISO 3166-1 alfa-2 | 2 letras, debe existir en `Pais` | FR |
| Origen | Código ISO 3166-1 alfa-2 | 2 letras, debe existir en `Pais` | ES |
| NombrePasajero | Nombre | Máx. 60 | Maria |
| PrimerApellido | Primer apellido | Máx. 60 | Garcia |
| SegundoApellido | Segundo apellido | Opcional, máx. 60 | Lopez |
| DocumentoIdentidad | Identificador único del pasajero | Obligatorio, máx. 20 | 10000001A |
| FechaReserva | Fecha de la reserva | Formato `yyyy-MM-dd` | 2026-05-14 |

Ejemplo:

```csv
Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva
LOC001,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-14
LOC001,FR,ES,Juan,Perez,Gomez,22222222B,2026-05-14
LOC002,IT,ES,Maria,Garcia,Lopez,66666666F,2026-05-14
LOC003,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-15
```

### Reglas de validación

Las filas incorrectas se omiten con un aviso (línea o localizador y motivo); el resto se importa.

| Caso | Resultado |
|---|---|
| Fila vacía | Se ignora |
| Distinto de 8 campos | Aviso con número de línea |
| Fecha no válida | Aviso con número de línea |
| Longitudes superadas o localizador/documento vacío | Aviso con número de línea |
| Código ISO inexistente (p. ej. `UK`; Reino Unido es `GB`) | Aviso con el localizador |
| Mismo localizador con distinto origen, destino o fecha | Se rechaza el localizador completo |
| Localizador que ya existe en la BD | Se omite (no se sobrescribe ni se duplica) |
| Documento repetido dentro del mismo localizador | Se guarda una sola vez |
| Mismo documento en distintos localizadores | Un único pasajero asociado a varias reservas |

---

## Ficheros exportados

Desde **Reservas**, cada combinación de destino y fecha se exporta con el formato `AirTortilla_XX_YYYY_MM_DD.<ext>` y se descarga en el navegador.

| Formato | Contenido |
|---|---|
| `.csv` | Cabecera + una línea por pasajero, separada por comas |
| `.json` | Lista de objetos con sangría (`JsonSerializer`) |
| `.xml` | Lista de objetos (`XmlSerializer`) |
| `.txt` | Una línea por pasajero, campos separados por ` \| `, sin cabecera |

En CSV y TXT la fecha se escribe como `yyyy-MM-dd`. En JSON y XML se serializa el `DateTime` completo (`2026-05-14T00:00:00`).

**Ordenación:** los grupos se ordenan por destino y fecha; dentro de cada fichero, por `Localizador` y, a igualdad, por `DocumentoIdentidad`. La ordenación la hace MySQL (`ORDER BY`).

---

## Rendimiento y complejidad

Siendo *n* el número de filas del CSV:

| Fase | Operación | Complejidad |
|---|---|---|
| Lectura y validación de formato | Recorrer las *n* líneas | O(n) |
| Comprobación de países, localizadores y pasajeros existentes | **3 consultas en bloque** + búsquedas en `HashSet` / `Dictionary` | O(n) |
| Agrupación por localizador | `GroupBy` | O(n) |
| Inserción | Un único `SaveChanges` por fase | O(n) |
| Consulta de un grupo | `WHERE` + `ORDER BY` en MySQL (índice `Destino, FechaReserva`) | O(k log k), *k* = filas del grupo |

**Decisión clave:** en lugar de consultar la base de datos por cada fila (3 × *n* consultas, el llamado problema N+1), se cargan de una vez los países, los localizadores y los pasajeros ya existentes, y se comprueba en memoria. Con 10.000 filas son 3 consultas en lugar de 30.000.

Otras decisiones:
- La auditoría muestra solo los 200 registros más recientes (`Take(200)`) y el filtro por acción se aplica en MySQL, no en memoria.
- Se usa `IDbContextFactory` (recomendación oficial para Blazor Server): cada operación crea y libera su propio contexto.

---

## Conclusión

La entrega 2 cumple las tres User Stories y la migración a base de datos relacional:

- **US1:** inicio y cierre de sesión con contraseñas en BCrypt, todas las páginas protegidas por defecto y accesos registrados.
- **US2:** alta, activación y desactivación de usuarios restringidas al rol Administrador.
- **US3:** auditoría de altas, modificaciones (con valores antes/después), accesos, importaciones y exportaciones, consultable desde la aplicación.
- **Migración:** todas las funciones de la entrega 1 (cargar CSV, dividir por destino y fecha, consultar en modalidad total y línea a línea, exportar a cuatro formatos) funcionan ahora sobre MySQL desde una interfaz web.

Se han aplicado los patrones **DTO**, **DAO** y **Factory**, y se ha resuelto la limitación de la entrega 1 de tener los códigos ISO escritos en el código.

### Limitaciones conocidas

- Los campos del CSV no pueden contener comas (la lectura no interpreta comillas).
- La importación carga en memoria las filas del CSV; para ficheros muy grandes convendría importar por lotes.
- La revalidación de la sesión se hace en cada **petición HTTP**. Si el usuario desactivado está dentro de una página interactiva (por ejemplo, Reservas), puede seguir usándola hasta que navegue, recargue o descargue un fichero; en ese momento se le expulsa.
- La revalidación hace una consulta a MySQL por petición. Para el volumen de la práctica es despreciable; con mucho tráfico convendría cachear el estado unos minutos.
- El usuario de MySQL `airtortilla_app` tiene permiso `DELETE` sobre todas las tablas, incluida `Auditoria`: desde la aplicación no se puede borrar, pero sí con acceso directo a la BD.
- `ReservasDTO` está duplicado entre el proyecto de consola y el web.
- No hay recuperación ni cambio de contraseña por parte del usuario.
- No se ha implementado el despliegue con contenedores (Docker).

### Posibles mejoras

- Revalidar también las páginas interactivas sin esperar a una petición HTTP (`RevalidatingServerAuthenticationStateProvider`).
- Dar a `airtortilla_app` solo `SELECT` e `INSERT` sobre `Auditoria`.
- Usar CsvHelper para admitir campos entre comillas e importar por lotes.
- Mover `ReservasDTO` a una biblioteca de clases compartida.
- Cambio de contraseña y bloqueo temporal tras varios intentos fallidos.
- Paginación en la auditoría y en las reservas.
- Pruebas unitarias (validación del CSV, DAO, exportadores).
- Contenedores Docker para la aplicación y MySQL (`docker compose`).
