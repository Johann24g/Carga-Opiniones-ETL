using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using CsvHelper;
using CsvHelper.Configuration;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;

namespace CargaOpiniones.Data.Services
{
    public class OpinionServiceAdo : IOpinion
    {
        private readonly string _connectionString = AppConfig.ConnectionString;

        public async Task<OperationResult> LoadOpinionAsync(string surveysPath, string socialCommentsPath, string webReviewsPath)
        {
            var stats = new OperationResult { EntityName = "Opiniones (ADO.NET)", Success = true };

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

                var existingClients = new HashSet<int>();
                var existingProducts = new HashSet<int>();
                var existingClasificaciones = new Dictionary<string, int>();
                var existingFuentes = new Dictionary<string, string>();
                var existingOpiniones = new HashSet<int>();
                int redSocialTipoFuenteId = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand("SELECT IdCliente FROM Clientes", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) existingClients.Add(reader.GetInt32(0));
                    }

                    using (var cmd = new SqlCommand("SELECT IdProducto FROM Productos", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) existingProducts.Add(reader.GetInt32(0));
                    }

                    using (var cmd = new SqlCommand("SELECT IdClasificacion, Nombre FROM Clasificacion", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingClasificaciones[reader.GetString(1).Trim().ToLower()] = reader.GetInt32(0);
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT fd.IdFuente, tf.Nombre FROM FuentesDatos fd LEFT JOIN TiposFuentes tf ON fd.IdTipoFuente = tf.IdTipoFuente", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string idF = reader.GetString(0).Trim();
                            string tfName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
                            existingFuentes[idF] = tfName;
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT IdTipoFuente FROM TiposFuentes WHERE Nombre = 'Red Social'", connection))
                    {
                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null) redSocialTipoFuenteId = (int)res;
                    }

                    using (var cmd = new SqlCommand("SELECT IdOpinion FROM Opiniones", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) existingOpiniones.Add(reader.GetInt32(0));
                    }
                }

                if (File.Exists(surveysPath))
                {
                    using (var reader = new StreamReader(surveysPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<SurveyCsvRecord>().ToList();

                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();

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
                                    surveyStats.Processed++;
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

                                    if (!existingClients.Contains(idCliente.Value))
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

                                if (!existingProducts.Contains(idProducto))
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

                                string csvFuenteId = existingFuentes.FirstOrDefault(f => f.Value.Equals("CSV", StringComparison.OrdinalIgnoreCase)).Key;
                                if (string.IsNullOrEmpty(csvFuenteId))
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

                                    if (!existingClasificaciones.TryGetValue(clasKey, out idClasificacion))
                                    {
                                        idClasificacion = existingClasificaciones.Count > 0 ? existingClasificaciones.Values.Max() + 1 : 1;
                                        using (var insertCmd = new SqlCommand("INSERT INTO Clasificacion (IdClasificacion, Nombre) VALUES (@Id, @Name)", connection))
                                        {
                                            insertCmd.Parameters.AddWithValue("@Id", idClasificacion);
                                            insertCmd.Parameters.AddWithValue("@Name", clasName);
                                            await insertCmd.ExecuteNonQueryAsync();
                                        }
                                        existingClasificaciones[clasKey] = idClasificacion;
                                    }
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

                                using (var cmd = new SqlCommand("dbo.sp_CargarOpiniones", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                                    cmd.Parameters.AddWithValue("@IdFuente", csvFuenteId);
                                    cmd.Parameters.AddWithValue("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                                    cmd.Parameters.AddWithValue("@Comentario", record.Comentario.Trim());
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = new SqlCommand("dbo.sp_CargarSurveys", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    cmd.Parameters.AddWithValue("@PuntajeSatisfaccion", puntaje.HasValue ? (object)puntaje.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IdClasificacion", idClasificacion);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                existingOpiniones.Add(idOpinion);
                                surveyStats.Inserted++;
                            }
                        }
                    }
                }

                if (File.Exists(socialCommentsPath))
                {
                    using (var reader = new StreamReader(socialCommentsPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<SocialCommentCsvRecord>().ToList();

                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();

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

                                    if (!existingClients.Contains(idCliente.Value))
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

                                if (!existingProducts.Contains(idProducto))
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

                                if (!existingFuentes.ContainsKey(platformName))
                                {
                                    if (redSocialTipoFuenteId > 0)
                                    {
                                        using (var insertCmd = new SqlCommand("INSERT INTO FuentesDatos (IdFuente, FechaCarga, IdTipoFuente) VALUES (@Id, @Fecha, @TipoId)", connection))
                                        {
                                            insertCmd.Parameters.AddWithValue("@Id", platformName);
                                            insertCmd.Parameters.AddWithValue("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                                            insertCmd.Parameters.AddWithValue("@TipoId", redSocialTipoFuenteId);
                                            await insertCmd.ExecuteNonQueryAsync();
                                        }
                                        existingFuentes[platformName] = "Red Social";
                                        stats.DynamicallySeededFuentes++;
                                    }
                                }

                                using (var cmd = new SqlCommand("dbo.sp_CargarOpiniones", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                                    cmd.Parameters.AddWithValue("@IdFuente", platformName);
                                    cmd.Parameters.AddWithValue("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                                    cmd.Parameters.AddWithValue("@Comentario", record.Comentario.Trim());
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = new SqlCommand("dbo.sp_CargarSocialComments", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                existingOpiniones.Add(idOpinion);
                                commentStats.Inserted++;
                            }
                        }
                    }
                }

                if (File.Exists(webReviewsPath))
                {
                    using (var reader = new StreamReader(webReviewsPath))
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        var records = csv.GetRecords<WebReviewCsvRecord>().ToList();

                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();

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

                                    if (!existingClients.Contains(idCliente.Value))
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

                                if (!existingProducts.Contains(idProducto))
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

                                string webFuenteId = existingFuentes.FirstOrDefault(f => f.Value.Equals("Web", StringComparison.OrdinalIgnoreCase)).Key;
                                if (string.IsNullOrEmpty(webFuenteId))
                                {
                                    stats.Rejected++;
                                    stats.Errors.Add($"Web Review Row {reviewStats.Processed}: No Web source found.");
                                    continue;
                                }

                                using (var cmd = new SqlCommand("dbo.sp_CargarOpiniones", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.HasValue ? (object)idCliente.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                                    cmd.Parameters.AddWithValue("@IdFuente", webFuenteId);
                                    cmd.Parameters.AddWithValue("@Fecha", fecha.ToDateTime(TimeOnly.MinValue));
                                    cmd.Parameters.AddWithValue("@Comentario", record.Comentario.Trim());
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = new SqlCommand("dbo.sp_CargarWebReviews", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@IdOpinion", idOpinion);
                                    cmd.Parameters.AddWithValue("@Rating", rating);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                existingOpiniones.Add(idOpinion);
                                reviewStats.Inserted++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading opinions (ADO.NET): {ex.Message}");
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
