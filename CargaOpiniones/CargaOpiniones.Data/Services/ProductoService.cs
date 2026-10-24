using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;

namespace CargaOpiniones.Data.Services
{
    public class ProductoService : IProducto
    {
        private readonly AnalisisOpinionesdbContext _context;

        public ProductoService(AnalisisOpinionesdbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult> LoadProductoAsync(string productsFilePath)
        {
            var stats = new OperationResult { EntityName = "Productos y Categorias (EF Core)", Success = true };

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
                var existingProducts = await _context.Productos.ToDictionaryAsync(p => p.IdProducto);

                using (var reader = new StreamReader(productsFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<ProductsCsvRecord>().ToList();

                    foreach (var record in records)
                    {
                        if (string.IsNullOrWhiteSpace(record.Categoría)) continue;

                        string catName = record.Categoría.Trim();
                        string catKey = catName.ToLower();

                        if (!existingCategorias.TryGetValue(catKey, out var categoria))
                        {
                            int newId = existingCategorias.Count > 0 ? existingCategorias.Values.Max(c => c.IdCategoria) + 1 : 1;
                            categoria = new Categoria { IdCategoria = newId, NombreCategoria = catName };
                            _context.Categorias.Add(categoria);
                            existingCategorias.Add(catKey, categoria);
                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var record in records)
                    {
                        stats.Processed++;

                        if (string.IsNullOrWhiteSpace(record.IdProducto) || string.IsNullOrWhiteSpace(record.Nombre) || string.IsNullOrWhiteSpace(record.Categoría))
                        {
                            stats.Rejected++;
                            stats.Errors.Add($"Row {stats.Processed}: Required fields missing.");
                            continue;
                        }

                        string cleanIdStr = Regex.Replace(record.IdProducto, @"[^\d]", "");
                        if (!int.TryParse(cleanIdStr, out int idProducto))
                        {
                            stats.Rejected++;
                            stats.Errors.Add($"Row {stats.Processed}: Invalid IdProducto format '{record.IdProducto}'.");
                            continue;
                        }

                        if (existingProducts.ContainsKey(idProducto))
                        {
                            stats.Rejected++;
                            stats.Errors.Add($"Row {stats.Processed}: Duplicate IdProducto {idProducto} already exists.");
                            continue;
                        }

                        string catKey = record.Categoría.Trim().ToLower();
                        var cat = existingCategorias[catKey];

                        var pIdProducto = new SqlParameter("@IdProducto", idProducto);
                        var pNombreProducto = new SqlParameter("@NombreProducto", record.Nombre.Trim());
                        var pIdCategoria = new SqlParameter("@IdCategoria", cat.IdCategoria);

                        await _context.Database.ExecuteSqlRawAsync("EXEC dbo.sp_CargarProductos @IdProducto, @NombreProducto, @IdCategoria", pIdProducto, pNombreProducto, pIdCategoria);

                        existingProducts.Add(idProducto, new Producto { IdProducto = idProducto, NombreProducto = record.Nombre.Trim(), IdCategoria = cat.IdCategoria });
                        stats.Inserted++;
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading products: {ex.Message}");
            }

            return stats;
        }
    }
}
