# AirTortilla · Gestión de reservas

Práctica de **Acceso a Datos (DAM)**. Sistema de gestión de reservas de la aerolínea AirTortilla.

- **Entrega 1:** aplicación de consola en C# que leía un CSV de reservas y lo dividía en ficheros por país de destino y fecha.
- **Entrega 2 (actual):** migración a **aplicación web (Blazor)** con **base de datos relacional (MySQL)**, inicio de sesión, gestión de usuarios por rol y trazabilidad de las modificaciones.

> El proyecto de consola de la entrega 1 se ha retirado del repositorio al completarse la migración. Su código sigue disponible en el historial de Git (Pull Request #1). El proyecto activo es `AirTortilla.Web/`.

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
| Despliegue | `dotnet run` | **Docker** + `docker compose` (MySQL 8.4 + aplicación) | Que cualquiera pueda ejecutar el proyecto sin instalar .NET ni MySQL. |

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

- `ReservasDTO` (con `GetCsvHeader`, `ToCsvLine`, `ToTxtLine`) se trasladó al proyecto web sin cambios de lógica para transportar los datos y generar los ficheros exportados.
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
docker-compose.yml         Contenedores MySQL + aplicación
.env                       Contraseñas de demostración para Docker
AirTortilla.Web/Dockerfile Cómo se compila la aplicación dentro de Docker
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
| 3.5 | Auditar importaciones y exportaciones | Cada importación genera `IMPORTAR` con el resumen (reservas, pasajeros nuevos, avisos de formato, avisos de datos y total); cada descarga genera `EXPORTAR` con el nombre del fichero | ✅ |
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
| M.6 | Contenedores (Docker) | En un equipo con solo Docker Desktop, `docker compose up -d --build` deja la aplicación funcionando en `http://localhost:8080` con la base de datos creada | ✅ |

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
| **Puerto de MySQL en Docker solo local** (`127.0.0.1:3307`) | Nadie de la misma red puede conectarse a la base de datos del contenedor. |
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

> **Para evaluar la práctica, usa la opción A (Docker).** No hace falta instalar .NET, MySQL ni Visual Studio: Docker crea la base de datos y arranca la aplicación con un solo comando.

### Opción A · Con Docker (recomendada)

#### Qué necesitas

| Programa | Para qué | ¿Hay que instalarlo? |
|---|---|---|
| **Docker Desktop** | Ejecutar la base de datos y la aplicación | Solo si no lo tienes: <https://www.docker.com/products/docker-desktop/> |
| **GitHub Desktop** | Descargar el proyecto | Solo si no lo tienes (alternativa: descargar el ZIP, ver paso 1) |
| Un navegador | Usar la aplicación | — |

No necesitas Git, .NET, MySQL ni Visual Studio.

#### Paso 1 · Descargar el proyecto

**Con GitHub Desktop:**
1. **File → Clone repository...** → pestaña **URL**.
2. Pega: `https://github.com/marianaangarita/ProyectoFicherosMarianaAngarita`
3. En **Local path** elige una carpeta **fuera de OneDrive** (por ejemplo `C:\Repos`). OneDrive puede bloquear archivos y provocar errores raros.
4. Pulsa **Clone**. Se descarga la rama principal (`master`), que contiene la entrega 2.

**Sin GitHub Desktop (ZIP):**
1. Abre <https://github.com/marianaangarita/ProyectoFicherosMarianaAngarita>.
2. Botón verde **Code → Download ZIP**.
3. Descomprime el ZIP en una carpeta fuera de OneDrive.

#### Paso 2 · Abrir Docker Desktop

Ábrelo desde el menú Inicio y espera a que abajo a la izquierda ponga **"Engine running"** (indicador verde).

#### Paso 3 · Abrir una terminal en la carpeta del proyecto

La carpeta correcta es la que contiene el archivo **`docker-compose.yml`**. Cualquiera de estas formas vale:

- **GitHub Desktop:** menú **Repository → Open in Command Prompt** (o *Open in PowerShell*).
- **Explorador de Windows:** entra en la carpeta del proyecto → clic derecho en un hueco vacío → **Abrir en Terminal**.
- **VS Code:** **File → Open Folder...** → elige la carpeta del proyecto → menú **Terminal → New Terminal**.

Comprueba que estás en el sitio correcto escribiendo `dir`: debe aparecer `docker-compose.yml`.

#### Paso 4 · Arrancar la aplicación

```
docker compose up -d --build
```

- La **primera vez** tarda varios minutos: descarga MySQL y .NET (alrededor de 1 GB). Las siguientes veces son segundos.
- Docker crea la base de datos, ejecuta el script `database/airtortilla_schema.sql` (tablas, roles y 249 países) y arranca la web.

Espera un minuto y comprueba el estado:

```
docker compose ps
```

Debe salir `airtortilla-db` como **Up (healthy)** y `airtortilla-web` como **Up**.

#### Paso 5 · Entrar en la aplicación

Abre **<http://localhost:8080>** e inicia sesión con el administrador de demostración:

| Usuario | Contraseña |
|---|---|
| `admin` | `AdminDemo2026` |

#### Paso 6 · Probar

1. **Importar CSV:** crea un archivo `reservas.csv` con el [ejemplo de prueba](#csv-de-prueba-para-copiar-y-pegar) y súbelo.
2. **Reservas:** consulta un grupo en modalidad *Total* y *Línea a línea*, y expórtalo en los cuatro formatos.
3. **Usuarios:** da de alta un usuario con rol *Empleado*, cierra sesión y entra con él: no verá *Usuarios* ni *Auditoría*.
4. **Auditoría** (como `admin`): verás registrados todos los pasos anteriores.

#### Apagar y volver a empezar

| Quiero... | Comando | ¿Se borran los datos? |
|---|---|---|
| Apagar la aplicación | `docker compose stop` | No |
| Volver a encenderla | `docker compose start` | — |
| Apagar y quitar los contenedores | `docker compose down` | No (quedan en el volumen) |
| **Empezar de cero** (base de datos vacía) | `docker compose down -v` y después `docker compose up -d --build` | **Sí** |

#### Si algo falla

| Síntoma | Solución |
|---|---|
| `docker: command not found` o error de conexión con Docker | Docker Desktop no está abierto o no ha terminado de arrancar. Espera a ver "Engine running". |
| `no configuration file provided` | La terminal no está en la carpeta del proyecto. Repite el paso 3. |
| Se cortó el primer arranque (se cerró Docker, se apagó el PC...) | La base de datos se quedó a medias: `docker compose down -v` y vuelve a lanzar `docker compose up -d --build`. |
| `airtortilla-web` aparece como *Restarting* | Mira el error con `docker compose logs web --tail 50`. Lo más habitual se soluciona con `docker compose down -v` y volviendo a arrancar. |
| `port is already allocated` (puerto 8080 ocupado) | Otro programa usa el puerto 8080. En `docker-compose.yml`, cambia `"8080:8080"` por `"8081:8080"` y entra en `http://localhost:8081`. |
| Los botones no responden (por ejemplo, *Importar* no se activa) | Recarga la página con `F5`. Si se reiniciaron los contenedores con la página abierta, se pierde la conexión. |

#### Sobre las contraseñas del archivo `.env`

El archivo `.env` contiene contraseñas **de demostración** (`RootDemo2026`, `AppDemo2026`, `AdminDemo2026`). Están en el repositorio a propósito, para que el proyecto funcione sin configurar nada. Solo sirven para la base de datos que Docker crea **en tu propio ordenador**, que además solo es accesible desde ese ordenador (`127.0.0.1`). No se usan en ningún otro sitio.

#### Qué hace Docker exactamente

`docker-compose.yml` define dos contenedores:

| Contenedor | Imagen | Detalles |
|---|---|---|
| `airtortilla-db` | `mysql:8.4` | Ejecuta el script de la base de datos la primera vez, crea el usuario `airtortilla_app`, guarda los datos en el volumen `airtortilla-datos`. Puerto `127.0.0.1:3307` (para conectar Workbench si se quiere). |
| `airtortilla-web` | Construida con `AirTortilla.Web/Dockerfile` | Compila y publica la aplicación con el SDK de .NET 10 y la ejecuta en el puerto `8080`. Espera a que MySQL esté listo (*healthcheck*). |

Ajustes necesarios para que funcione igual que en Windows:
- `--lower-case-table-names=1`: MySQL en Linux distingue mayúsculas en los nombres de tabla y en Windows no. Sin esta opción, la aplicación busca `usuario` y la tabla se llama `Usuario`.
- `TZ: Europe/Madrid`: sin ella, los contenedores usan la hora UTC y la auditoría registraría una hora menos.
- El `Dockerfile` restaura y publica en un solo paso: si se separan, .NET 10 no incluye `blazor.web.js` y las páginas dejan de ser interactivas.

### Opción B · Sin Docker (para desarrollar)

Para modificar el código con Visual Studio o VS Code.

#### Requisitos

- .NET 10 SDK (`dotnet --version`)
- MySQL Server 8 y MySQL Workbench

#### 1. Crear la base de datos

En MySQL Workbench, abrir y ejecutar `database/airtortilla_schema.sql` (botón ⚡). Crea la base de datos `airtortilla`, las tablas, los roles y los 249 países.

> ⚠️ El script empieza con `DROP DATABASE IF EXISTS airtortilla`: si se vuelve a ejecutar, **borra todos los datos**.

#### 2. Crear el usuario de la aplicación

```sql
CREATE USER 'airtortilla_app'@'localhost' IDENTIFIED BY '<contraseña>';
GRANT SELECT, INSERT, UPDATE, DELETE ON airtortilla.* TO 'airtortilla_app'@'localhost';
FLUSH PRIVILEGES;
```

#### 3. Configurar los secretos

Desde la carpeta `AirTortilla.Web/`:

```
dotnet user-secrets set "ConnectionStrings:AirTortilla" "server=localhost;port=3306;database=airtortilla;user=airtortilla_app;password=<contraseña>"
dotnet user-secrets set "AdminInicial:Password" "<contraseña del administrador>"
```

Los *user-secrets* se guardan en el perfil del usuario de Windows, fuera del repositorio.

#### 4. Ejecutar

```
cd AirTortilla.Web
dotnet run
```

Abrir la dirección que muestra la terminal (`http://localhost:5223`). En el primer arranque se crea el usuario `admin` con la contraseña del paso 3. Si falta algún secreto, la aplicación se detiene con un mensaje que indica cuál.

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

### CSV de prueba para copiar y pegar

Los CSV de prueba no se incluyen en el repositorio. Para crear uno:

1. Abre el **Bloc de notas** (o VS Code).
2. Copia y pega el contenido siguiente.
3. **Archivo → Guardar como...** → nombre `reservas.csv`, tipo **Todos los archivos**, codificación **UTF-8**.

```csv
Localizador,Destino,Origen,NombrePasajero,PrimerApellido,SegundoApellido,DocumentoIdentidad,FechaReserva
LOC001,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-14
LOC001,FR,ES,Juan,Perez,Gomez,22222222B,2026-05-14
LOC001,FR,ES,Juan,Perez,Gomez,33333333C,2026-05-14
LOC001,FR,ES,Juan,Perez,Gomez,44444444D,2026-05-14
LOC001,FR,ES,Juan,Perez,Gomez,55555555E,2026-05-14
LOC002,IT,ES,Maria,Garcia,Lopez,66666666F,2026-05-14
LOC002,IT,ES,Maria,Garcia,Lopez,77777777G,2026-05-14
LOC003,FR,ES,Ana,Martin,Ruiz,88888888H,2026-05-15
LOC003,FR,ES,Ana,Martin,Ruiz,99999999J,2026-05-15
LOC004,FR,ES,Juan,Perez,Gomez,11111111A,2026-05-15
LOC005,DE,PT,Lucas,Silva,,12345678K,2026-05-16
LOC006,UK,ES,Peter,Smith,,23456789L,2026-05-16
LOC007,FR,ES,Laura,Sanz,Gil,34567890M,2026-13-45
LOC008,FR,ES,Carlos,Ruiz
LOC009,FR,ES,Elena,Diaz,Mora,45678901N,2026-05-17
LOC009,IT,ES,Pablo,Diaz,Mora,56789012P,2026-05-17
```

**Resultado esperado:** 5 reservas importadas, 10 pasajeros nuevos y 4 avisos:

| Fila | Qué comprueba | Resultado |
|---|---|---|
| LOC001 | 5 pasajeros que coinciden en nombre, origen y destino | Se importa |
| LOC004 | El pasajero `11111111A` ya está en LOC001 | Se importa; el pasajero **no** se duplica |
| LOC005 | Sin segundo apellido | Se importa |
| LOC006 | `UK` no es un código ISO (Reino Unido es `GB`) | Aviso |
| LOC007 | Fecha `2026-13-45` no válida | Aviso |
| LOC008 | Faltan columnas | Aviso |
| LOC009 | Mismo localizador con dos destinos distintos | Aviso |

Si se importa **otra vez** el mismo archivo, el resultado es 0 reservas importadas: la importación no duplica datos.

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

- **Docker:** la aplicación y la base de datos se arrancan con un solo comando, sin instalar .NET ni MySQL.

Se han aplicado los patrones **DTO**, **DAO** y **Factory**, y se ha resuelto la limitación de la entrega 1 de tener los códigos ISO escritos en el código.

### Limitaciones conocidas

- Los campos del CSV no pueden contener comas (la lectura no interpreta comillas).
- La importación carga en memoria las filas del CSV; para ficheros muy grandes convendría importar por lotes.
- La revalidación de la sesión se hace en cada **petición HTTP**. Si el usuario desactivado está dentro de una página interactiva (por ejemplo, Reservas), puede seguir usándola hasta que navegue, recargue o descargue un fichero; en ese momento se le expulsa.
- La revalidación hace una consulta a MySQL por petición. Para el volumen de la práctica es despreciable; con mucho tráfico convendría cachear el estado unos minutos.
- El usuario de MySQL `airtortilla_app` tiene permiso `DELETE` sobre todas las tablas, incluida `Auditoria`: desde la aplicación no se puede borrar, pero sí con acceso directo a la BD.
- No hay recuperación ni cambio de contraseña por parte del usuario.
- El archivo `.env` con contraseñas de demostración está en el repositorio para facilitar la evaluación. En un entorno real no se subiría y cada instalación tendría sus propias contraseñas.

### Posibles mejoras

- Revalidar también las páginas interactivas sin esperar a una petición HTTP (`RevalidatingServerAuthenticationStateProvider`).
- Dar a `airtortilla_app` solo `SELECT` e `INSERT` sobre `Auditoria`.
- Usar CsvHelper para admitir campos entre comillas e importar por lotes.
- Cambio de contraseña y bloqueo temporal tras varios intentos fallidos.
- Paginación en la auditoría y en las reservas.
- Pruebas unitarias (validación del CSV, DAO, exportadores).
- Publicar la imagen de la aplicación en un registro (Docker Hub o GitHub Container Registry) para no tener que compilarla en cada equipo.
