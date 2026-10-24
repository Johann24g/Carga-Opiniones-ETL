USE AnalisisOpinionesdb;
GO

SELECT 'Categorias'     AS Tabla, COUNT(*) AS Registros FROM dbo.Categorias
UNION ALL SELECT 'Productos',      COUNT(*) FROM dbo.Productos
UNION ALL SELECT 'Clientes',       COUNT(*) FROM dbo.Clientes
UNION ALL SELECT 'TiposFuentes',   COUNT(*) FROM dbo.TiposFuentes
UNION ALL SELECT 'FuentesDatos',   COUNT(*) FROM dbo.FuentesDatos
UNION ALL SELECT 'Clasificacion',  COUNT(*) FROM dbo.Clasificacion
UNION ALL SELECT 'Opiniones',      COUNT(*) FROM dbo.Opiniones
UNION ALL SELECT 'Surveys',        COUNT(*) FROM dbo.Surveys
UNION ALL SELECT 'SocialComments', COUNT(*) FROM dbo.SocialComments
UNION ALL SELECT 'WebReviews',     COUNT(*) FROM dbo.WebReviews;

SELECT  SUM(CASE WHEN o.IdCliente IS NOT NULL AND c.IdCliente IS NULL THEN 1 ELSE 0 END) AS OpinionesConClienteInexistente,
        SUM(CASE WHEN p.IdProducto IS NULL THEN 1 ELSE 0 END)                            AS OpinionesConProductoInexistente,
        SUM(CASE WHEN f.IdFuente   IS NULL THEN 1 ELSE 0 END)                            AS OpinionesConFuenteInexistente
FROM    dbo.Opiniones o
        LEFT JOIN dbo.Clientes     c ON c.IdCliente  = o.IdCliente
        LEFT JOIN dbo.Productos    p ON p.IdProducto = o.IdProducto
        LEFT JOIN dbo.FuentesDatos f ON f.IdFuente   = o.IdFuente;

SELECT (SELECT COUNT(*) FROM dbo.Productos p
         LEFT JOIN dbo.Categorias c ON c.IdCategoria = p.IdCategoria
         WHERE c.IdCategoria IS NULL)                              AS ProductosSinCategoria,
       (SELECT COUNT(*) FROM dbo.FuentesDatos f
         LEFT JOIN dbo.TiposFuentes t ON t.IdTipoFuente = f.IdTipoFuente
         WHERE t.IdTipoFuente IS NULL)                             AS FuentesSinTipo;

SELECT o.IdOpinion,
       (CASE WHEN s.IdOpinion  IS NOT NULL THEN 1 ELSE 0 END
      + CASE WHEN sc.IdOpinion IS NOT NULL THEN 1 ELSE 0 END
      + CASE WHEN w.IdOpinion  IS NOT NULL THEN 1 ELSE 0 END) AS TiposAsignados
FROM   dbo.Opiniones o
       LEFT JOIN dbo.Surveys        s  ON s.IdOpinion  = o.IdOpinion
       LEFT JOIN dbo.SocialComments sc ON sc.IdOpinion = o.IdOpinion
       LEFT JOIN dbo.WebReviews     w  ON w.IdOpinion  = o.IdOpinion
WHERE  (CASE WHEN s.IdOpinion  IS NOT NULL THEN 1 ELSE 0 END
      + CASE WHEN sc.IdOpinion IS NOT NULL THEN 1 ELSE 0 END
      + CASE WHEN w.IdOpinion  IS NOT NULL THEN 1 ELSE 0 END) <> 1;

SELECT Email, COUNT(*) AS Veces
FROM   dbo.Clientes
GROUP  BY Email
HAVING COUNT(*) > 1;

SELECT (SELECT COUNT(*) FROM dbo.Surveys    WHERE PuntajeSatisfaccion NOT BETWEEN 1 AND 5) AS PuntajesInvalidos,
       (SELECT COUNT(*) FROM dbo.WebReviews WHERE Rating              NOT BETWEEN 1 AND 5) AS RatingsInvalidos;

SELECT TipoOpinion, COUNT(*) AS Total
FROM   dbo.vw_OpinionesCompletas
GROUP  BY TipoOpinion
ORDER  BY Total DESC;

SELECT COUNT(*) AS OpinionesAnonimas
FROM   dbo.Opiniones
WHERE  IdCliente IS NULL;

SELECT TOP 5 IdProducto, NombreProducto, Categoria, TotalOpiniones, RatingPromedioWeb
FROM   dbo.vw_ResumenPorProducto
ORDER  BY TotalOpiniones DESC, IdProducto;

SELECT (SELECT COUNT(*) FROM dbo.Clientes  WHERE Nombre         LIKE 'Cliente Autogenerado%')  AS ClientesAutogenerados,
       (SELECT COUNT(*) FROM dbo.Productos WHERE NombreProducto LIKE 'Producto Autogenerado%') AS ProductosAutogenerados;
GO
