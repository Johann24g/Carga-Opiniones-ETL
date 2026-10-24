IF DB_ID(N'AnalisisOpinionesdb') IS NULL
    CREATE DATABASE AnalisisOpinionesdb;
GO

USE AnalisisOpinionesdb;
GO

IF OBJECT_ID(N'dbo.Categorias', N'U') IS NULL
CREATE TABLE dbo.Categorias (
    IdCategoria     INT          NOT NULL,
    NombreCategoria VARCHAR(80)  NOT NULL,
    CONSTRAINT PK_CATEGORIAS PRIMARY KEY (IdCategoria),
    CONSTRAINT UQ_CATEGORIAS_NOMBRE UNIQUE (NombreCategoria)
);
GO

IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
CREATE TABLE dbo.Productos (
    IdProducto     INT          NOT NULL,
    NombreProducto VARCHAR(120) NOT NULL,
    IdCategoria    INT          NOT NULL,
    CONSTRAINT PK_PRODUCTOS PRIMARY KEY (IdProducto),
    CONSTRAINT FK_PRODUCTOS_CATEGORIAS FOREIGN KEY (IdCategoria)
        REFERENCES dbo.Categorias (IdCategoria)
);
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
CREATE TABLE dbo.Clientes (
    IdCliente INT          NOT NULL,
    Nombre    VARCHAR(100) NOT NULL,
    Email     VARCHAR(150) NOT NULL,
    CONSTRAINT PK_CLIENTES PRIMARY KEY (IdCliente),
    CONSTRAINT UQ_CLIENTES_EMAIL UNIQUE (Email)
);
GO

IF OBJECT_ID(N'dbo.TiposFuentes', N'U') IS NULL
CREATE TABLE dbo.TiposFuentes (
    IdTipoFuente INT         NOT NULL,
    Nombre       VARCHAR(50) NOT NULL,
    CONSTRAINT PK_TIPOSFUENTES PRIMARY KEY (IdTipoFuente),
    CONSTRAINT UQ_TIPOSFUENTES_NOMBRE UNIQUE (Nombre)
);
GO

IF OBJECT_ID(N'dbo.FuentesDatos', N'U') IS NULL
CREATE TABLE dbo.FuentesDatos (
    IdFuente     VARCHAR(10) NOT NULL,
    FechaCarga   DATE        NOT NULL,
    IdTipoFuente INT         NULL,
    CONSTRAINT PK_FUENTESDATOS PRIMARY KEY (IdFuente),
    CONSTRAINT FK_FUENTESDATOS_TIPOSFUENTES FOREIGN KEY (IdTipoFuente)
        REFERENCES dbo.TiposFuentes (IdTipoFuente)
);
GO

IF OBJECT_ID(N'dbo.Clasificacion', N'U') IS NULL
CREATE TABLE dbo.Clasificacion (
    IdClasificacion INT         NOT NULL,
    Nombre          VARCHAR(80) NOT NULL,
    CONSTRAINT PK_CLASIFICACION PRIMARY KEY (IdClasificacion),
    CONSTRAINT UQ_CLASIFICACION_NOMBRE UNIQUE (Nombre)
);
GO

IF OBJECT_ID(N'dbo.Opiniones', N'U') IS NULL
CREATE TABLE dbo.Opiniones (
    IdOpinion  INT          NOT NULL,
    IdCliente  INT          NULL,
    IdProducto INT          NOT NULL,
    IdFuente   VARCHAR(10)  NOT NULL,
    Fecha      DATE         NOT NULL,
    Comentario VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_OPINIONES PRIMARY KEY (IdOpinion),
    CONSTRAINT FK_OPINIONES_CLIENTES  FOREIGN KEY (IdCliente)  REFERENCES dbo.Clientes (IdCliente),
    CONSTRAINT FK_OPINIONES_PRODUCTOS FOREIGN KEY (IdProducto) REFERENCES dbo.Productos (IdProducto),
    CONSTRAINT FK_OPINIONES_FUENTES   FOREIGN KEY (IdFuente)   REFERENCES dbo.FuentesDatos (IdFuente)
);
GO

IF OBJECT_ID(N'dbo.Surveys', N'U') IS NULL
CREATE TABLE dbo.Surveys (
    IdOpinion           INT NOT NULL,
    PuntajeSatisfaccion INT NULL,
    IdClasificacion     INT NOT NULL,
    CONSTRAINT PK_SURVEYS PRIMARY KEY (IdOpinion),
    CONSTRAINT FK_SURVEYS_OPINIONES FOREIGN KEY (IdOpinion) REFERENCES dbo.Opiniones (IdOpinion),
    CONSTRAINT FK_SURVEYS_CLASIFICACION FOREIGN KEY (IdClasificacion) REFERENCES dbo.Clasificacion (IdClasificacion),
    CONSTRAINT CK_SURVEYS_PUNTAJE CHECK (PuntajeSatisfaccion IS NULL OR PuntajeSatisfaccion BETWEEN 1 AND 5)
);
GO

IF OBJECT_ID(N'dbo.SocialComments', N'U') IS NULL
CREATE TABLE dbo.SocialComments (
    IdOpinion INT NOT NULL,
    CONSTRAINT PK_SOCIALCOMMENTS PRIMARY KEY (IdOpinion),
    CONSTRAINT FK_SOCIALCOMMENTS_OPINIONES FOREIGN KEY (IdOpinion) REFERENCES dbo.Opiniones (IdOpinion)
);
GO

IF OBJECT_ID(N'dbo.WebReviews', N'U') IS NULL
CREATE TABLE dbo.WebReviews (
    IdOpinion INT NOT NULL,
    Rating    INT NOT NULL,
    CONSTRAINT PK_WEBREVIEWS PRIMARY KEY (IdOpinion),
    CONSTRAINT FK_WEBREVIEWS_OPINIONES FOREIGN KEY (IdOpinion) REFERENCES dbo.Opiniones (IdOpinion),
    CONSTRAINT CK_WEBREVIEWS_RATING CHECK (Rating BETWEEN 1 AND 5)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Opiniones_IdProducto')
    CREATE INDEX IX_Opiniones_IdProducto ON dbo.Opiniones (IdProducto);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Opiniones_IdCliente')
    CREATE INDEX IX_Opiniones_IdCliente ON dbo.Opiniones (IdCliente);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Opiniones_IdFuente')
    CREATE INDEX IX_Opiniones_IdFuente ON dbo.Opiniones (IdFuente);
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarProductos
    @IdProducto     INT,
    @NombreProducto VARCHAR(120),
    @IdCategoria    INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Productos (IdProducto, NombreProducto, IdCategoria)
    VALUES (@IdProducto, @NombreProducto, @IdCategoria);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarFuentesDatos
    @IdFuente     VARCHAR(10),
    @FechaCarga   DATE,
    @IdTipoFuente INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.FuentesDatos (IdFuente, FechaCarga, IdTipoFuente)
    VALUES (@IdFuente, @FechaCarga, @IdTipoFuente);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarOpiniones
    @IdOpinion  INT,
    @IdCliente  INT = NULL,
    @IdProducto INT,
    @IdFuente   VARCHAR(10),
    @Fecha      DATE,
    @Comentario VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Opiniones (IdOpinion, IdCliente, IdProducto, IdFuente, Fecha, Comentario)
    VALUES (@IdOpinion, @IdCliente, @IdProducto, @IdFuente, @Fecha, @Comentario);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarSurveys
    @IdOpinion           INT,
    @PuntajeSatisfaccion INT = NULL,
    @IdClasificacion     INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Surveys (IdOpinion, PuntajeSatisfaccion, IdClasificacion)
    VALUES (@IdOpinion, @PuntajeSatisfaccion, @IdClasificacion);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarSocialComments
    @IdOpinion INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.SocialComments (IdOpinion) VALUES (@IdOpinion);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CargarWebReviews
    @IdOpinion INT,
    @Rating    INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.WebReviews (IdOpinion, Rating) VALUES (@IdOpinion, @Rating);
END
GO

CREATE OR ALTER VIEW dbo.vw_OpinionesCompletas
AS
SELECT  o.IdOpinion,
        CASE WHEN s.IdOpinion  IS NOT NULL THEN 'Encuesta'
             WHEN sc.IdOpinion IS NOT NULL THEN 'Red Social'
             WHEN w.IdOpinion  IS NOT NULL THEN 'Reseña Web'
             ELSE 'Sin clasificar' END           AS TipoOpinion,
        o.Fecha,
        c.IdCliente,
        c.Nombre                                 AS Cliente,
        p.IdProducto,
        p.NombreProducto                         AS Producto,
        cat.NombreCategoria                      AS Categoria,
        o.IdFuente,
        tf.Nombre                                AS TipoFuente,
        o.Comentario,
        s.PuntajeSatisfaccion,
        cl.Nombre                                AS Clasificacion,
        w.Rating
FROM    dbo.Opiniones o
        JOIN dbo.Productos   p   ON p.IdProducto  = o.IdProducto
        JOIN dbo.Categorias  cat ON cat.IdCategoria = p.IdCategoria
        JOIN dbo.FuentesDatos fd ON fd.IdFuente   = o.IdFuente
        LEFT JOIN dbo.TiposFuentes tf ON tf.IdTipoFuente = fd.IdTipoFuente
        LEFT JOIN dbo.Clientes  c  ON c.IdCliente  = o.IdCliente
        LEFT JOIN dbo.Surveys   s  ON s.IdOpinion  = o.IdOpinion
        LEFT JOIN dbo.Clasificacion cl ON cl.IdClasificacion = s.IdClasificacion
        LEFT JOIN dbo.SocialComments sc ON sc.IdOpinion = o.IdOpinion
        LEFT JOIN dbo.WebReviews w ON w.IdOpinion = o.IdOpinion;
GO

CREATE OR ALTER VIEW dbo.vw_ResumenPorProducto
AS
SELECT  p.IdProducto,
        p.NombreProducto,
        cat.NombreCategoria                         AS Categoria,
        COUNT(o.IdOpinion)                          AS TotalOpiniones,
        CAST(AVG(CAST(w.Rating AS DECIMAL(5,2))) AS DECIMAL(5,2))              AS RatingPromedioWeb,
        CAST(AVG(CAST(s.PuntajeSatisfaccion AS DECIMAL(5,2))) AS DECIMAL(5,2)) AS SatisfaccionPromedioEncuesta
FROM    dbo.Productos p
        JOIN dbo.Categorias cat ON cat.IdCategoria = p.IdCategoria
        LEFT JOIN dbo.Opiniones o ON o.IdProducto = p.IdProducto
        LEFT JOIN dbo.WebReviews w ON w.IdOpinion = o.IdOpinion
        LEFT JOIN dbo.Surveys s ON s.IdOpinion = o.IdOpinion
GROUP BY p.IdProducto, p.NombreProducto, cat.NombreCategoria;
GO

CREATE OR ALTER VIEW dbo.vw_OpinionesPorFuente
AS
SELECT  fd.IdFuente,
        tf.Nombre          AS TipoFuente,
        COUNT(o.IdOpinion) AS TotalOpiniones
FROM    dbo.FuentesDatos fd
        LEFT JOIN dbo.TiposFuentes tf ON tf.IdTipoFuente = fd.IdTipoFuente
        LEFT JOIN dbo.Opiniones o ON o.IdFuente = fd.IdFuente
GROUP BY fd.IdFuente, tf.Nombre;
GO

PRINT 'Base de datos AnalisisOpinionesdb lista.';
GO
