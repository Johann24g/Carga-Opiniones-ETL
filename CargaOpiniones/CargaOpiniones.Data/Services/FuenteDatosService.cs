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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Services
{
    public class FuenteDatosService : IFuenteDatos
    {
        private readonly AnalisisOpinionesdbContext _context;

        public FuenteDatosService(AnalisisOpinionesdbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult> LoadFuenteDatosAsync(string fuenteDatosFilePath)
        {
            var stats = new OperationResult { EntityName = "Fuentes de Datos (EF Core)", Success = true };

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

                var existingTiposFuentes = await _context.TiposFuentes.ToDictionaryAsync(t => t.Nombre.ToLower());
                var existingFuentesDatos = await _context.FuentesDatos.ToDictionaryAsync(f => f.IdFuente);

                using (var reader = new StreamReader(fuenteDatosFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<FuenteDatosCsvRecord>().ToList();
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
                        if (existingFuentesDatos.ContainsKey(idFuente))
                        {
                            stats.Rejected++;
                            stats.Errors.Add($"Row {stats.Processed}: Duplicate IdFuente '{idFuente}' already exists.");
                            continue;
                        }

                        string tipoFuenteName = record.TipoFuente.Trim();
                        string tipoFuenteKey = tipoFuenteName.ToLower();
                        if (!existingTiposFuentes.TryGetValue(tipoFuenteKey, out var tipoFuente))
                        {
                            int newId = existingTiposFuentes.Count > 0 ? existingTiposFuentes.Values.Max(t => t.IdTipoFuente) + 1 : 1;
                            tipoFuente = new TiposFuente { IdTipoFuente = newId, Nombre = tipoFuenteName };
                            _context.TiposFuentes.Add(tipoFuente);
                            existingTiposFuentes.Add(tipoFuenteKey, tipoFuente);
                            await _context.SaveChangesAsync();
                        }

                        var pIdFuente = new SqlParameter("@IdFuente", idFuente);
                        var pFechaCarga = new SqlParameter("@FechaCarga", fechaCarga.ToDateTime(TimeOnly.MinValue));
                        var pIdTipoFuente = new SqlParameter("@IdTipoFuente", tipoFuente.IdTipoFuente);

                        await _context.Database.ExecuteSqlRawAsync("EXEC dbo.sp_CargarFuentesDatos @IdFuente, @FechaCarga, @IdTipoFuente", pIdFuente, pFechaCarga, pIdTipoFuente);

                        existingFuentesDatos.Add(idFuente, new FuentesDato { IdFuente = idFuente, FechaCarga = fechaCarga, IdTipoFuente = tipoFuente.IdTipoFuente });
                        stats.Inserted++;
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading data sources: {ex.Message}");
            }

            return stats;
        }
    }
}
