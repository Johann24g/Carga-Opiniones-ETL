using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public class CategoriaService : ICategoria
    {
        private readonly AnalisisOpinionesdbContext _context;

        public CategoriaService(AnalisisOpinionesdbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult> LoadCategoriaAsync(string productsFilePath)
        {
            var stats = new OperationResult { EntityName = "Categorias (EF Core)", Success = true };

            try
            {
                if (!File.Exists(productsFilePath))
                {
                    stats.Rejected++;
                    stats.Errors.Add($"File '{productsFilePath}' not found.");
                    return stats;
                }

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower()
                };

                var existingCategorias = await _context.Categorias.ToDictionaryAsync(c => c.NombreCategoria.ToLower());

                using (var reader = new StreamReader(productsFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<ProductsCsvRecord>().ToList();
                    foreach (var record in records)
                    {
                        if (string.IsNullOrWhiteSpace(record.Categoría)) continue;

                        string catName = record.Categoría.Trim();
                        string catKey = catName.ToLower();

                        if (!existingCategorias.ContainsKey(catKey))
                        {
                            stats.Processed++;
                            int newId = existingCategorias.Count > 0 ? existingCategorias.Values.Max(c => c.IdCategoria) + 1 : 1;
                            var categoria = new Categoria { IdCategoria = newId, NombreCategoria = catName };
                            _context.Categorias.Add(categoria);
                            existingCategorias.Add(catKey, categoria);
                            stats.Inserted++;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading categories: {ex.Message}");
            }

            return stats;
        }
    }
}
