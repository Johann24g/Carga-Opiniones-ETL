using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Services
{
    public class FuenteDatosServiceAdo : IFuenteDatos
    {
        private readonly string _connectionString = AppConfig.ConnectionString;

        public async Task<OperationResult> LoadFuenteDatosAsync(string fuenteDatosFilePath)
        {
            var stats = new OperationResult { EntityName = "Fuentes de Datos (ADO.NET)", Success = true };

            try
            {
                if (!File.Exists(fuenteDatosFilePath))
                {
                    stats.Rejected++;
                    stats.Errors.Add($"File '{fuenteDatosFilePath}' not found.");
                    return stats;
                }

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower()
                };

                var existingTiposFuentes = new Dictionary<string, int>();
                var existingFuentesDatos = new HashSet<string>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand("SELECT IdTipoFuente, Nombre FROM TiposFuentes", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingTiposFuentes[reader.GetString(1).Trim().ToLower()] = reader.GetInt32(0);
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT IdFuente FROM FuentesDatos", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingFuentesDatos.Add(reader.GetString(0).Trim());
                        }
                    }
                }

                using (var reader = new StreamReader(fuenteDatosFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<FuenteDatosCsvRecord>().ToList();

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        foreach (var record in records)
                        {
                            stats.Processed++;

                            if (string.IsNullOrWhiteSpace(record.IdFuente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: IdFuente is required.");
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(record.TipoFuente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: TipoFuente is required.");
                                continue;
                            }
                            if (!DateOnly.TryParseExact(record.FechaCarga, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaCarga))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Invalid FechaCarga format ('{record.FechaCarga}').");
                                continue;
                            }

                            string idFuente = record.IdFuente.Trim();
                            if (existingFuentesDatos.Contains(idFuente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Duplicate IdFuente '{idFuente}' already exists.");
                                continue;
                            }

                            string tipoFuenteName = record.TipoFuente.Trim();
                            string tipoFuenteKey = tipoFuenteName.ToLower();

                            if (!existingTiposFuentes.TryGetValue(tipoFuenteKey, out int tipoFuenteId))
                            {
                                tipoFuenteId = existingTiposFuentes.Count > 0 ? existingTiposFuentes.Values.Max() + 1 : 1;
                                using (var insertCmd = new SqlCommand("INSERT INTO TiposFuentes (IdTipoFuente, Nombre) VALUES (@Id, @Name)", connection))
                                {
                                    insertCmd.Parameters.AddWithValue("@Id", tipoFuenteId);
                                    insertCmd.Parameters.AddWithValue("@Name", tipoFuenteName);
                                    await insertCmd.ExecuteNonQueryAsync();
                                }
                                existingTiposFuentes[tipoFuenteKey] = tipoFuenteId;
                            }

                            using (var cmd = new SqlCommand("dbo.sp_CargarFuentesDatos", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@IdFuente", idFuente);
                                cmd.Parameters.AddWithValue("@FechaCarga", fechaCarga.ToDateTime(TimeOnly.MinValue));
                                cmd.Parameters.AddWithValue("@IdTipoFuente", tipoFuenteId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            existingFuentesDatos.Add(idFuente);
                            stats.Inserted++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading data sources (ADO.NET): {ex.Message}");
            }

            return stats;
        }
    }
}
