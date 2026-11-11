# AirTortilla · Gestión de reservas

## Índice

1. [Objetivos de la entrega](#objetivos-de-la-entrega)
2. [Instalación y ejecución](#instalación-y-ejecución)
3. [Formato del CSV](#formato-del-csv)
4. [User Stories y tareas medibles](#user-stories-y-tareas-medibles)
5. [Migración de tecnología](#migración-de-tecnología)
6. [Arquitectura](#arquitectura)
7. [Modelo de datos](#modelo-de-datos)
8. [Seguridad](#seguridad)
9. [Patrones de diseño](#patrones-de-diseño)
10. [Conclusión](#conclusión)
    
---
## Objetivos de la entrega

| ID | User Story |
|---|---|
| **US1** | Como **Empleado** quiero poder hacer *log in* para acceder a los recursos de la compañía. |
| **US2** | Como **Administrador** quiero poder dar de alta usuarios para garantizar la seguridad interna de la aplicación. |
| **US3** | Como **Administrador** quiero mantener la trazabilidad de las modificaciones. |

Además, se migra toda la persistencia a una **base de datos relacional** y se añaden contenedores (Docker).

---

## Instalación y ejecución

Solo necesitas **Docker Desktop** y **GitHub Desktop** (o descargar el ZIP).

### 1. Descargar el proyecto

- **Con GitHub Desktop:** pulsa **Code → Open with GitHub Desktop** y después **Clone**. Puedes dejar la carpeta que propone, salvo que esté dentro de OneDrive (en ese caso elige otra, por ejemplo `C:\Repos`).
- **Sin GitHub Desktop:** **Code → Download ZIP** y descomprímelo fuera de OneDrive.

### 2. Abrir Docker Desktop

Espera a que ponga **"Engine running"**.

### 3. Abrir el proyecto en Visual Studio Code

1. En GitHub Desktop, pulsa **Open in Visual Studio Code** (o en VS Code: **File → Open Folder...** y elige la carpeta del proyecto).
2. Abre una terminal: menú **Terminal → New Terminal**.

La terminal se abre directamente en la carpeta del proyecto, la que contiene `docker-compose.yml`.

### 4. Arrancar

```
docker compose up -d --build
```

La primera vez tarda unos minutos (descarga MySQL y .NET). Después comprueba que los dos contenedores están en marcha:

```
docker compose ps
```

`airtortilla-db` debe aparecer como **Up (healthy)** y `airtortilla-web` como **Up**.

### 5. Entrar

Abre **<http://localhost:8080>** con el usuario **`admin`** y la contraseña **`AdminDemo2026`**.

Para probar, importa el [CSV de prueba](#csv-de-prueba), consulta y exporta las reservas, da de alta un usuario *Empleado* y revisa la *Auditoría*.


### Comandos útiles

| Quiero... | Comando |
|---|---|
| Apagar | `docker compose stop` |
| Volver a encender | `docker compose start` |
| Empezar de cero (borra los datos) | `docker compose down -v` y después `docker compose up -d --build` |

### Si algo falla

| Problema | Solución |
|---|---|
| Error de conexión con Docker | Docker Desktop no está abierto o aún no pone "Engine running". |
| `no configuration file provided` | La terminal no está en la carpeta del proyecto. |
| Se cortó el primer arranque o `airtortilla-web` sale como *Restarting* | `docker compose down -v` y vuelve a arrancar. |
| Puerto 8080 ocupado | En `docker-compose.yml` cambia `"8080:8080"` por `"8081:8080"` y entra en `localhost:8081`. |
| Los botones no responden | Recarga la página con `F5`. |

> Las contraseñas del archivo `.env` son **de demostración**: solo sirven para la base de datos que Docker crea en tu propio ordenador, accesible únicamente desde él.

---

## Formato del CSV

Mismo formato que la entrega 1: primera línea de cabecera, separado por comas y sin comas dentro de los campos.

| Columna | Restricción | Ejemplo |
|---|---|---|
| Localizador | Obligatorio, máx. 20 | LOC001 |
| Destino / Origen | Código ISO de 2 letras (Reino Unido es `GB`, no `UK`) | FR / ES |
| NombrePasajero, PrimerApellido, SegundoApellido | Máx. 60 (el segundo apellido es opcional) | Juan, Perez, Gomez |
| DocumentoIdentidad | Obligatorio, máx. 20 | 11111111A |
| FechaReserva | `yyyy-MM-dd` | 2026-05-14 |

Las filas incorrectas se omiten con un aviso que indica la línea y el motivo. Los localizadores que ya existen no se vuelven a importar.

### CSV de prueba

Los CSV no se incluyen en el repositorio. Copia esto en el Bloc de notas y guárdalo como `reservas.csv`:

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

**Resultado esperado:** 5 reservas importadas, 10 pasajeros nuevos y 4 avisos (LOC006 con un código ISO incorrecto, LOC007 con una fecha imposible, LOC008 con columnas que faltan y LOC009 con dos destinos distintos). El pasajero `11111111A` aparece en LOC001 y LOC004, pero se guarda una sola vez.

Cada grupo de destino y fecha se exporta como `AirTortilla_XX_YYYY_MM_DD` en CSV, JSON, XML o TXT.

---

## User Stories y tareas medibles

| US | Tarea | Cómo se comprueba |
|---|---|---|
| US1 | Contraseñas con BCrypt | En la tabla `usuario` de la base de datos, la contraseña no se ve: aparece un código cifrado que empieza por `$2a$11$` |
| US1 | Iniciar y cerrar sesión | Con usuario y contraseña correctos, la aplicación te saluda por tu nombre. Si alguno de los dos está mal, muestra el mensaje "Usuario o contraseña incorrectos" |
| US1 | Páginas protegidas | Si alguien que no ha iniciado sesión intenta entrar en una página, por ejemplo escribiendo `localhost:8080/reservas`, la aplicación le manda a la pantalla de login |
| US2 | Alta de usuarios solo para el Administrador | Un usuario con rol Empleado no ve la opción *Usuarios* en el menú. Si escribe la dirección `localhost:8080/usuarios` a mano, la aplicación le muestra "Acceso denegado" |
| US2 | Validación del alta | El nombre debe tener entre 3 y 50 caracteres, la contraseña al menos 8, el rol es obligatorio y no puede haber dos usuarios con el mismo nombre |
| US2 | Activar y desactivar usuarios | El Administrador puede desactivar a un usuario desde la página *Usuarios*. Ese usuario ya no puede iniciar sesión, y si en ese momento estaba usando la aplicación, se le saca al login en cuanto cambia de página |
| US3 | Registro de auditoría | Cada inicio de sesión (correcto o fallido), alta, modificación, importación y exportación queda apuntado: quién lo hizo, cuándo y qué cambió |
| US3 | Consulta de la auditoría | Solo el Administrador ve la página *Auditoría*. Muestra todo lo apuntado, permite filtrar por tipo de acción y no deja editar ni borrar nada |
| BD | Migración a base de datos | Al importar el CSV de prueba se guardan 5 reservas y aparecen 4 avisos. Si se importa otra vez, no se duplica nada |
| BD | Consultar y exportar | En *Reservas* se puede ver cada grupo entero (*Total*) o línea a línea, y descargarlo en CSV, JSON, XML o TXT |
|  Extra  | Docker | Con solo Docker Desktop instalado, la aplicación funciona en `localhost:8080` |

---

## Migración de tecnología

En esta entrega se ha cambiado el stack tecnológico. Estos son los cambios respecto a la entrega 1:

| Capa | Entrega 1 | Entrega 2 |
|---|---|---|
| Interfaz | Consola | **Blazor Server** (web) |
| Persistencia | Ficheros CSV/JSON/XML/TXT | **MySQL 8** |
| Acceso a datos | `StreamReader` / `File` | **Entity Framework Core 10** |
| Seguridad | — | Login con cookie, roles y **BCrypt** |
| Ejecución | `dotnet run` | **Docker Compose** |

Las funciones de la entrega 1 se mantienen:

| Consola (entrega 1) | Web (entrega 2) |
|---|---|
| Opción 1: leer CSV y generar ficheros | **Importar CSV** guarda en MySQL; los ficheros se generan al **exportar** |
| Opción 2: consulta total | Botón **Total** |
| Opción 3: línea a línea (`ENTER` / `Q`) | Botón **Línea a línea** ("Siguiente" / "Salir") |
| Códigos ISO en un `HashSet` del código | Tabla `Pais` en la base de datos |

El proyecto de consola se ha eliminado del repositorio; su código sigue en el historial de Git (Pull Request #1).

---

## Arquitectura

![Diagrama de Arquitectura](assets/AirTortilla_Arquitectura_E2_V3.drawio.png)

---

## Modelo de datos

![Diagrama Entidad-Relación](assets/AirTortilla_ER.drawio.png)

Script: [`database/airtortilla_schema.sql`](database/airtortilla_schema.sql). Tiene siete tablas: `Pais`, `Reserva`, `Pasajero`, `ReservaPasajero`, `Rol`, `Usuario` y `Auditoria`.

Decisiones principales:
- **Reserva N:M Pasajero**, resuelta con la tabla intermedia `ReservaPasajero`. Un pasajero que viaja en dos reservas se guarda una sola vez.
- **Origen, destino y fecha son de la reserva**: todos los pasajeros de un localizador viajan juntos.
- **Los usuarios se desactivan, no se borran**, para no perder su rastro en la auditoría.
- **Cada usuario guarda qué administrador lo dio de alta** (columna `CreadoPorId`, que apunta a otro usuario de la misma tabla).

---

## Seguridad

- **Contraseñas con BCrypt:** solo se guarda el hash.
- **Mismo mensaje para cualquier fallo de login:** no revela qué usuarios existen.
- **Todas las páginas protegidas por defecto**, y **Usuarios** y **Auditoría** solo para el rol Administrador.
- **Sesión revalidada en cada petición:** un usuario desactivado es expulsado.
- **Protección CSRF** en formularios y cierre de sesión.
- **Ninguna contraseña en el código:** *user-secrets* en desarrollo y `.env` en Docker.
- **La aplicación no usa la cuenta de administrador de MySQL (`root`):** usa una cuenta propia que solo tiene acceso a la base de datos de AirTortilla.
- **La base de datos de Docker solo acepta conexiones desde el propio ordenador:** nadie conectado a la misma red (por ejemplo, la wifi de clase) puede entrar en ella.

---

## Patrones de diseño

- **DTO** (`AirTortilla.Web/DTO/`): `ReservasDTO` transporta los datos entre el CSV, la base de datos y los ficheros exportados.
- **DAO** (`AirTortilla.Web/Data/DAO/`): las páginas usan la interfaz `IReservaDao`; la implementación (`ReservaDaoMySql`) se elige en una línea de `Program.cs`. Volver a ficheros solo requeriría otra implementación.
- **Factory** (`AirTortilla.Web/Data/Exportacion/`): `ExportadorFactory.Crear("json")` devuelve el exportador adecuado. Añadir un formato es una clase nueva más una línea en la fábrica.
  
---

## Conclusión

La aplicación cumple las tres User Stories: los empleados inician sesión, el administrador da de alta y gestiona usuarios, y todos los cambios quedan registrados en la auditoría.

Además, los datos ya no se guardan en ficheros sino en una base de datos MySQL. Todo lo que hacía la entrega 1 (importar el CSV, consultar las reservas y exportarlas) se puede hacer ahora desde una página web.

Por último, gracias a Docker, cualquiera puede ponerla en marcha con un solo comando, sin instalar nada más que Docker Desktop.

**Limitaciones conocidas**
- Los campos del CSV no pueden contener comas.
- Un usuario desactivado que esté en una página abierta no es expulsado hasta que navega o recarga.
- El usuario de MySQL de la aplicación podría borrar la auditoría con acceso directo a la base de datos (desde la aplicación no es posible).
- El `.env` con contraseñas de demostración está en el repositorio para facilitar la evaluación; en un entorno real no se subiría.

**Posibles mejoras**
- Admitir campos entre comillas en el CSV (CsvHelper).
- Cambio de contraseña y bloqueo tras varios intentos fallidos.
- Paginación en la auditoría y en las reservas.
- Pruebas unitarias.
