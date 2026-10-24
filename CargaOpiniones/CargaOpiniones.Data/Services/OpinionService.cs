using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;

namespace CargaOpiniones.Data.Services
{
    public class OpinionService : IOpinion
    {
        private readonly AnalisisOpinionesdbContext _context;

        public OpinionService(AnalisisOpinionesdbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult> LoadOpinionAsync(string surveysPath, string socialCommentsPath, string webReviewsPath)
        {
            var stats = new OperationResult { EntityName = "Opiniones (EF Core)", Success = true };

            var surveyStats = new OperationResult { EntityName = "Encuestas (Surveys)", Success = true };
            var commentStats = new OperationResult { EntityName = "Redes Sociales (Comments)", Success = true };
            var reviewStats = new OperationResult { EntityName = "Reseñas Web (Reviews)", Success = true };

            try
            {
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower()
                };

                var existingClients = await _context.Clientes.ToDictionaryAsync(c => c.IdCliente);
                var existingProducts = await _context.Productos.ToDictionaryAsync(p => p.IdProducto);
                var existingClasificaciones = await _context.Clasificacions.ToDictionaryAsync(c => c.Nombre.ToLower());
                var existingTiposFuentesRef = await _context.TiposFuentes.ToDictionaryAsync(t => t.Nombre.ToLower());
                var existingFuentesDatosRef = await _context.FuentesDatos.ToDictionaryAsync(f => f.IdFuente);
                var existingOpiniones = new HashSet<int>(await _context.Opiniones.Select(o => o.IdOpinion).ToListAsync());

                var fuentesList = existingFuentesDatosRef.Values
                    .Select(f => new { f.IdFuente, TipoFuente = existingTiposFuentesRef.Values.FirstOrDefault(t => t.IdTipoFuente == f.IdTipoFuente)?.Nombre })
                    .ToList();

                if (File.Exists(surveysPath))
                {
                    using (var reader = new StreamReader(surveysPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<SurveyCsvRecord>().ToList();
                        foreach (var record in records)
                        {
                            surveyStats.Processed++;

                            if (string.IsNullOrWhiteSpace(record.IdOpinion) || string.IsNullOrWhiteSpace(record.IdProducto) || string.IsNullOrWhiteSpace(record.Comentario))
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Required fields missing.");
                                continue;
                            }

                            if (!int.TryParse(record.IdOpinion, out int idOpinion))
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Invalid IdOpinion.");
                                continue;
                            }

                            if (existingOpiniones.Contains(idOpinion))
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Duplicate IdOpinion {idOpinion}.");
                                continue;
                            }

                            int? idCliente = null;
                            if (!string.IsNullOrWhiteSpace(record.IdCliente))
                            {
                                string cleanClientStr = Regex.Replace(record.IdCliente, @"[^\d]", "");
                                if (int.TryParse(cleanClientStr, out int cId)) idCliente = cId;
                                else
                                {
                                    surveyStats.Rejected++;
                                    surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Invalid IdCliente.");
                                    continue;
                                }

                                if (!existingClients.ContainsKey(idCliente.Value))
{
    surveyStats.Rejected++;
    surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: IdCliente {idCliente.Value} no existe en Clientes (integridad referencial).");
    continue;
}
                            }

                            string cleanProductStr = Regex.Replace(record.IdProducto, @"[^\d]", "");
                            if (!int.TryParse(cleanProductStr, out int idProducto))
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Invalid IdProducto.");
                                continue;
                            }

                            if (!existingProducts.ContainsKey(idProducto))
{
    surveyStats.Rejected++;
    surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: IdProducto {idProducto} no existe en Productos (integridad referencial).");
    continue;
}

                            if (!DateOnly.TryParseExact(record.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Invalid Fecha.");
                                continue;
                            }

                            var csvFuente = fuentesList.FirstOrDefault(f => f.TipoFuente != null && f.TipoFuente.Equals("CSV", StringComparison.OrdinalIgnoreCase));
                            if (csvFuente == null)
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: No CSV source found.");
                                continue;
                            }

                            int idClasificacion = 0;
                            if (!string.IsNullOrWhiteSpace(record.Clasificación))
                            {
                                string clasName = record.Clasificación.Trim();
                                string clasKey = clasName.ToLower();
                                if (!existingClasificaciones.TryGetValue(clasKey, out var clas))
                                {
                                    int newId = existingClasificaciones.Count > 0 ? existingClasificaciones.Values.Max(c => c.IdClasificacion) + 1 : 1;
                                    clas = new Clasificacion { IdClasificacion = newId, Nombre = clasName };
                                    _context.Clasificacions.Add(clas);
                                    existingClasificaciones.Add(clasKey, clas);
                                    await _context.SaveChangesAsync();
                                }
                                idClasificacion = clas.IdClasificacion;
                            }
                            else
                            {
                                surveyStats.Rejected++;
                                surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Clasificación is required.");
                                continue;
                            }

                            int? puntaje = null;
                            if (!string.IsNullOrWhiteSpace(record.PuntajeSatisfacción))
                            {
                                if (int.TryParse(record.PuntajeSatisfacción, out int p))
                                {
                                    if (p < 1 || p > 5)
                                    {
                                        surveyStats.Rejected++;
                                        surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: PuntajeSatisfacción must be 1-5.");
                                        continue;
                                    }
                                    puntaje = p;
                                }
                                else
                                {
                                    surveyStats.Rejected++;
                                    surveyStats.Errors.Add($"Survey Row {surveyStats.Processed}: Invalid PuntajeSatisfacción.");
                                    continue;
                                }
                            }

                            var pIdOpinion = new SqlParameter("@IdOpinion", idOpinion);
                            var pIdCliente = new SqlParameter("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                            var pIdProducto = new SqlParameter("@IdProducto", idProducto);
                            var pIdFuente = new SqlParameter("@IdFuente", csvFuente.IdFuente);
                            var pFecha = new SqlParameter("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                            var pComentario = new SqlParameter("@Comentario", record.Comentario.Trim());

                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC dbo.sp_CargarOpiniones @IdOpinion, @IdCliente, @IdProducto, @IdFuente, @Fecha, @Comentario",
                                pIdOpinion, pIdCliente, pIdProducto, pIdFuente, pFecha, pComentario);

                            var pIdOpinionS = new SqlParameter("@IdOpinion", idOpinion);
                            var pPuntaje = new SqlParameter("@PuntajeSatisfaccion", puntaje.HasValue ? (object)puntaje.Value : DBNull.Value);
                            var pIdClas = new SqlParameter("@IdClasificacion", idClasificacion);

                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC dbo.sp_CargarSurveys @IdOpinion, @PuntajeSatisfaccion, @IdClasificacion",
                                pIdOpinionS, pPuntaje, pIdClas);

                            existingOpiniones.Add(idOpinion);
                            surveyStats.Inserted++;
                        }
                    }
                }

                if (File.Exists(socialCommentsPath))
                {
                    using (var reader = new StreamReader(socialCommentsPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<SocialCommentCsvRecord>().ToList();
                        foreach (var record in records)
                        {
                            commentStats.Processed++;

                            if (string.IsNullOrWhiteSpace(record.IdComment) || string.IsNullOrWhiteSpace(record.IdProducto) || string.IsNullOrWhiteSpace(record.Comentario))
                            {
                                commentStats.Rejected++;
                                commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Required fields missing.");
                                continue;
                            }

                            string cleanCommentIdStr = Regex.Replace(record.IdComment, @"[^\d]", "");
                            if (!int.TryParse(cleanCommentIdStr, out int numericId))
                            {
                                commentStats.Rejected++;
                                commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Invalid IdComment.");
                                continue;
                            }
                            int idOpinion = numericId + 10000;

                            if (existingOpiniones.Contains(idOpinion))
                            {
                                commentStats.Rejected++;
                                commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Duplicate IdOpinion.");
                                continue;
                            }

                            int? idCliente = null;
                            if (!string.IsNullOrWhiteSpace(record.IdCliente))
                            {
                                string cleanClientStr = Regex.Replace(record.IdCliente, @"[^\d]", "");
                                if (int.TryParse(cleanClientStr, out int cId)) idCliente = cId;
                                else
                                {
                                    commentStats.Rejected++;
                                    commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Invalid IdCliente.");
                                    continue;
                                }

                                if (!existingClients.ContainsKey(idCliente.Value))
{
    commentStats.Rejected++;
    commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: IdCliente {idCliente.Value} no existe en Clientes (integridad referencial).");
    continue;
}
                            }

                            string cleanProductStr = Regex.Replace(record.IdProducto, @"[^\d]", "");
                            if (!int.TryParse(cleanProductStr, out int idProducto))
                            {
                                commentStats.Rejected++;
                                commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Invalid IdProducto.");
                                continue;
                            }

                            if (!existingProducts.ContainsKey(idProducto))
{
    commentStats.Rejected++;
    commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: IdProducto {idProducto} no existe en Productos (integridad referencial).");
    continue;
}

                            if (!DateOnly.TryParseExact(record.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                            {
                                commentStats.Rejected++;
                                commentStats.Errors.Add($"Social Comment Row {commentStats.Processed}: Invalid Fecha.");
                                continue;
                            }

                            string platformName = string.IsNullOrWhiteSpace(record.Fuente) ? "Red Social" : record.Fuente.Trim();
                            if (platformName.Length > 10) platformName = platformName.Substring(0, 10);

                            if (!existingFuentesDatosRef.ContainsKey(platformName))
                            {
                                var rsFuenteType = existingTiposFuentesRef.Values.FirstOrDefault(t => t.Nombre.Equals("Red Social", StringComparison.OrdinalIgnoreCase));
                                if (rsFuenteType != null)
                                {
                                    var dynamicFuente = new FuentesDato
                                    {
                                        IdFuente = platformName,
                                        FechaCarga = fecha,
                                        IdTipoFuente = rsFuenteType.IdTipoFuente
                                    };
                                    _context.FuentesDatos.Add(dynamicFuente);
                                    existingFuentesDatosRef[platformName] = dynamicFuente;
                                    await _context.SaveChangesAsync();
                                    stats.DynamicallySeededFuentes++;
                                }
                            }

                            var pIdOpinion = new SqlParameter("@IdOpinion", idOpinion);
                            var pIdCliente = new SqlParameter("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                            var pIdProducto = new SqlParameter("@IdProducto", idProducto);
                            var pIdFuente = new SqlParameter("@IdFuente", platformName);
                            var pFecha = new SqlParameter("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                            var pComentario = new SqlParameter("@Comentario", record.Comentario.Trim());

                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC dbo.sp_CargarOpiniones @IdOpinion, @IdCliente, @IdProducto, @IdFuente, @Fecha, @Comentario",
                                pIdOpinion, pIdCliente, pIdProducto, pIdFuente, pFecha, pComentario);

                            var pIdOpinionC = new SqlParameter("@IdOpinion", idOpinion);
                            await _context.Database.ExecuteSqlRawAsync("EXEC dbo.sp_CargarSocialComments @IdOpinion", pIdOpinionC);

                            existingOpiniones.Add(idOpinion);
                            commentStats.Inserted++;
                        }
                    }
                }

                if (File.Exists(webReviewsPath))
                {
                    using (var reader = new StreamReader(webReviewsPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<WebReviewCsvRecord>().ToList();
                        foreach (var record in records)
                        {
                            reviewStats.Processed++;

                            if (string.IsNullOrWhiteSpace(record.IdReview) || string.IsNullOrWhiteSpace(record.IdProducto) || string.IsNullOrWhiteSpace(record.Comentario) || string.IsNullOrWhiteSpace(record.Rating))
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Required fields missing.");
                                continue;
                            }

                            string cleanReviewIdStr = Regex.Replace(record.IdReview, @"[^\d]", "");
                            if (!int.TryParse(cleanReviewIdStr, out int numericId))
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Invalid IdReview.");
                                continue;
                            }
                            int idOpinion = numericId + 20000;

                            if (existingOpiniones.Contains(idOpinion))
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Duplicate IdOpinion.");
                                continue;
                            }

                            int? idCliente = null;
                            if (!string.IsNullOrWhiteSpace(record.IdCliente))
                            {
                                string cleanClientStr = Regex.Replace(record.IdCliente, @"[^\d]", "");
                                if (int.TryParse(cleanClientStr, out int cId)) idCliente = cId;
                                else
                                {
                                    reviewStats.Rejected++;
                                    reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Invalid IdCliente.");
                                    continue;
                                }

                                if (!existingClients.ContainsKey(idCliente.Value))
{
    reviewStats.Rejected++;
    reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: IdCliente {idCliente.Value} no existe en Clientes (integridad referencial).");
    continue;
}
                            }

                            string cleanProductStr = Regex.Replace(record.IdProducto, @"[^\d]", "");
                            if (!int.TryParse(cleanProductStr, out int idProducto))
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Invalid IdProducto.");
                                continue;
                            }

                            if (!existingProducts.ContainsKey(idProducto))
{
    reviewStats.Rejected++;
    reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: IdProducto {idProducto} no existe en Productos (integridad referencial).");
    continue;
}

                            if (!DateOnly.TryParseExact(record.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Invalid Fecha.");
                                continue;
                            }

                            if (!int.TryParse(record.Rating, out int rating) || rating < 1 || rating > 5)
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: Invalid Rating.");
                                continue;
                            }

                            var webFuente = fuentesList.FirstOrDefault(f => f.TipoFuente != null && f.TipoFuente.Equals("Web", StringComparison.OrdinalIgnoreCase));
                            if (webFuente == null)
                            {
                                reviewStats.Rejected++;
                                reviewStats.Errors.Add($"Web Review Row {reviewStats.Processed}: No Web source found.");
                                continue;
                            }

                            var pIdOpinion = new SqlParameter("@IdOpinion", idOpinion);
                            var pIdCliente = new SqlParameter("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                            var pIdProducto = new SqlParameter("@IdProducto", idProducto);
                            var pIdFuente = new SqlParameter("@IdFuente", webFuente.IdFuente);
                            var pFecha = new SqlParameter("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                            var pComentario = new SqlParameter("@Comentario", record.Comentario.Trim());

                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC dbo.sp_CargarOpiniones @IdOpinion, @IdCliente, @IdProducto, @IdFuente, @Fecha, @Comentario",
                                pIdOpinion, pIdCliente, pIdProducto, pIdFuente, pFecha, pComentario);

                            var pIdOpinionW = new SqlParameter("@IdOpinion", idOpinion);
                            var pRating = new SqlParameter("@Rating", rating);

                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC dbo.sp_CargarWebReviews @IdOpinion, @Rating",
                                pIdOpinionW, pRating);

                            existingOpiniones.Add(idOpinion);
                            reviewStats.Inserted++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading opinions: {ex.Message}");
            }

            stats.Processed = surveyStats.Processed + commentStats.Processed + reviewStats.Processed;
            stats.Inserted = surveyStats.Inserted + commentStats.Inserted + reviewStats.Inserted;
            stats.Rejected = surveyStats.Rejected + commentStats.Rejected + reviewStats.Rejected;
            stats.Success = stats.Success && surveyStats.Success && commentStats.Success && reviewStats.Success;
            stats.Errors.AddRange(surveyStats.Errors);
            stats.Errors.AddRange(commentStats.Errors);
            stats.Errors.AddRange(reviewStats.Errors);

            stats.Details.Add(surveyStats);
            stats.Details.Add(commentStats);
            stats.Details.Add(reviewStats);

            return stats;
        }
    }
}
