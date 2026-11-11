-- =====================================================================
--  AirTortilla - Entrega 2 - Esquema de base de datos (MySQL 8)
-- =====================================================================
DROP DATABASE IF EXISTS airtortilla;
CREATE DATABASE airtortilla CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE airtortilla;

-- ---------------------------------------------------------------------
--  DOMINIO: RESERVAS
-- ---------------------------------------------------------------------

-- Catálogo de países ISO 3166-1 alfa-2 (sustituye al HashSet del código)
CREATE TABLE Pais (
    CodigoIso CHAR(2)     NOT NULL,
    PRIMARY KEY (CodigoIso)
);

-- Pasajero: identificado de forma única por su documento
CREATE TABLE Pasajero (
    Id                 INT          NOT NULL AUTO_INCREMENT,
    DocumentoIdentidad VARCHAR(20)  NOT NULL,
    Nombre             VARCHAR(60)  NOT NULL,
    PrimerApellido     VARCHAR(60)  NOT NULL,
    SegundoApellido    VARCHAR(60)  NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UQ_Pasajero_Documento (DocumentoIdentidad)
);

-- Reserva: una por localizador (origen, destino y fecha son de la reserva)
CREATE TABLE Reserva (
    Id            INT         NOT NULL AUTO_INCREMENT,
    Localizador   VARCHAR(20) NOT NULL,
    Origen        CHAR(2)     NOT NULL,
    Destino       CHAR(2)     NOT NULL,
    FechaReserva  DATE        NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UQ_Reserva_Localizador (Localizador),
    KEY IX_Reserva_Destino_Fecha (Destino, FechaReserva),   -- para exportar por país y fecha
    CONSTRAINT FK_Reserva_Origen  FOREIGN KEY (Origen)  REFERENCES Pais (CodigoIso),
    CONSTRAINT FK_Reserva_Destino FOREIGN KEY (Destino) REFERENCES Pais (CodigoIso)
);

-- Relación N:M: una reserva tiene varios pasajeros y un pasajero puede tener varias reservas
CREATE TABLE ReservaPasajero (
    ReservaId  INT NOT NULL,
    PasajeroId INT NOT NULL,
    PRIMARY KEY (ReservaId, PasajeroId),
    CONSTRAINT FK_RP_Reserva  FOREIGN KEY (ReservaId)  REFERENCES Reserva (Id)  ON DELETE CASCADE,
    CONSTRAINT FK_RP_Pasajero FOREIGN KEY (PasajeroId) REFERENCES Pasajero (Id) ON DELETE RESTRICT
);

-- ---------------------------------------------------------------------
--  DOMINIO: SEGURIDAD (User Stories de login y alta de usuarios)
-- ---------------------------------------------------------------------

CREATE TABLE Rol (
    Id     INT         NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(30) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UQ_Rol_Nombre (Nombre)
);

CREATE TABLE Usuario (
    Id            INT          NOT NULL AUTO_INCREMENT,
    NombreUsuario VARCHAR(50)  NOT NULL,
    PasswordHash  VARCHAR(100) NOT NULL,          -- hash BCrypt, nunca la contraseña
    RolId         INT          NOT NULL,
    Activo        BOOLEAN      NOT NULL DEFAULT TRUE,  -- dar de baja sin borrar (no rompe la auditoría)
    FechaAlta     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPorId   INT          NULL,              -- qué administrador lo dio de alta
    UltimoAcceso  DATETIME     NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UQ_Usuario_Nombre (NombreUsuario),
    CONSTRAINT FK_Usuario_Rol       FOREIGN KEY (RolId)       REFERENCES Rol (Id),
    CONSTRAINT FK_Usuario_CreadoPor FOREIGN KEY (CreadoPorId) REFERENCES Usuario (Id)
);

-- ---------------------------------------------------------------------
--  DOMINIO: TRAZABILIDAD (User Story de auditoría)
-- ---------------------------------------------------------------------

CREATE TABLE Auditoria (
    Id                BIGINT      NOT NULL AUTO_INCREMENT,
    UsuarioId         INT         NULL,           -- quién (NULL = login fallido con usuario inexistente)
    Fecha             DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- cuándo
    Accion            ENUM('INSERT','UPDATE','DELETE','LOGIN','LOGIN_FALLIDO','IMPORTAR','EXPORTAR') NOT NULL,
    Tabla             VARCHAR(50) NULL,           -- sobre qué
    RegistroId        VARCHAR(50) NULL,
    ValoresAnteriores JSON        NULL,           -- antes
    ValoresNuevos     JSON        NULL,           -- después
    PRIMARY KEY (Id),
    KEY IX_Auditoria_Usuario_Fecha (UsuarioId, Fecha),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuario (Id)
);

-- ---------------------------------------------------------------------
--  DATOS INICIALES
-- ---------------------------------------------------------------------
INSERT INTO Rol (Nombre) VALUES ('Administrador'), ('Empleado');

-- El usuario administrador inicial lo creará la aplicación al arrancar
-- (así el hash BCrypt se genera en C# y no hay contraseñas en este script).

INSERT INTO Pais (CodigoIso) VALUES
('AD'),('AE'),('AF'),('AG'),('AI'),('AL'),('AM'),('AO'),('AQ'),('AR'),('AS'),('AT'),('AU'),('AW'),('AX'),('AZ'),
('BA'),('BB'),('BD'),('BE'),('BF'),('BG'),('BH'),('BI'),('BJ'),('BL'),('BM'),('BN'),('BO'),('BQ'),('BR'),('BS'),('BT'),('BV'),('BW'),('BY'),('BZ'),
('CA'),('CC'),('CD'),('CF'),('CG'),('CH'),('CI'),('CK'),('CL'),('CM'),('CN'),('CO'),('CR'),('CU'),('CV'),('CW'),('CX'),('CY'),('CZ'),
('DE'),('DJ'),('DK'),('DM'),('DO'),('DZ'),
('EC'),('EE'),('EG'),('EH'),('ER'),('ES'),('ET'),
('FI'),('FJ'),('FK'),('FM'),('FO'),('FR'),
('GA'),('GB'),('GD'),('GE'),('GF'),('GG'),('GH'),('GI'),('GL'),('GM'),('GN'),('GP'),('GQ'),('GR'),('GS'),('GT'),('GU'),('GW'),('GY'),
('HK'),('HM'),('HN'),('HR'),('HT'),('HU'),
('ID'),('IE'),('IL'),('IM'),('IN'),('IO'),('IQ'),('IR'),('IS'),('IT'),
('JE'),('JM'),('JO'),('JP'),
('KE'),('KG'),('KH'),('KI'),('KM'),('KN'),('KP'),('KR'),('KW'),('KY'),('KZ'),
('LA'),('LB'),('LC'),('LI'),('LK'),('LR'),('LS'),('LT'),('LU'),('LV'),('LY'),
('MA'),('MC'),('MD'),('ME'),('MF'),('MG'),('MH'),('MK'),('ML'),('MM'),('MN'),('MO'),('MP'),('MQ'),('MR'),('MS'),('MT'),('MU'),('MV'),('MW'),('MX'),('MY'),('MZ'),
('NA'),('NC'),('NE'),('NF'),('NG'),('NI'),('NL'),('NO'),('NP'),('NR'),('NU'),('NZ'),
('OM'),
('PA'),('PE'),('PF'),('PG'),('PH'),('PK'),('PL'),('PM'),('PN'),('PR'),('PS'),('PT'),('PW'),('PY'),
('QA'),
('RE'),('RO'),('RS'),('RU'),('RW'),
('SA'),('SB'),('SC'),('SD'),('SE'),('SG'),('SH'),('SI'),('SJ'),('SK'),('SL'),('SM'),('SN'),('SO'),('SR'),('SS'),('ST'),('SV'),('SX'),('SY'),('SZ'),
('TC'),('TD'),('TF'),('TG'),('TH'),('TJ'),('TK'),('TL'),('TM'),('TN'),('TO'),('TR'),('TT'),('TV'),('TW'),('TZ'),
('UA'),('UG'),('UM'),('US'),('UY'),('UZ'),
('VA'),('VC'),('VE'),('VG'),('VI'),('VN'),('VU'),
('WF'),('WS'),
('YE'),('YT'),
('ZA'),('ZM'),('ZW');
