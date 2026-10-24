using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Services
{
    public class ProductServiceAdoc : IProducto
    {
        private readonly string _connectionString = AppConfig.ConnectionString;

        public async Task<OperationResult> LoadProductoAsync(string productsFilePath)
        {
            var stats = new OperationResult { EntityName = "Productos y Categorias (ADO.NET)", Success = true };

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

                var existingCategorias = new Dictionary<string, int>();
                var existingProducts = new HashSet<int>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SELECT IdCategoria, NombreCategoria FROM Categorias", connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                existingCategorias[name.Trim().ToLower()] = id;
                            }
                        }
                    }
                    using (var command = new SqlCommand("SELECT IdProducto FROM Productos", connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingProducts.Add(reader.GetInt32(0));
                        }
                    }
                }

                using (var reader = new StreamReader(productsFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<ProductsCsvRecord>().ToList();

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        foreach (var record in records)
                        {
                            if (string.IsNullOrWhiteSpace(record.Categoría)) continue;

                            string catName = record.Categoría.Trim();
                            string catKey = catName.ToLower();

                            if (!existingCategorias.ContainsKey(catKey))
                            {
                                int newId = existingCategorias.Count > 0 ? existingCategorias.Values.Max() + 1 : 1;

                                using (var insertCommand = new SqlCommand("INSERT INTO Categorias (IdCategoria, NombreCategoria) VALUES (@Id, @Name)", connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@Id", newId);
                                    insertCommand.Parameters.AddWithValue("@Name", catName);
                                    await insertCommand.ExecuteNonQueryAsync();
                                }

                                existingCategorias[catKey] = newId;
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

                            if (existingProducts.Contains(idProducto))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Duplicate IdProducto {idProducto} already exists.");
                                continue;
                            }

                            string catKey = record.Categoría.Trim().ToLower();
                            int catId = existingCategorias[catKey];

                            using (var cmd = new SqlCommand("dbo.sp_CargarProductos", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                                cmd.Parameters.AddWithValue("@NombreProducto", record.Nombre.Trim());
                                cmd.Parameters.AddWithValue("@IdCategoria", catId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            existingProducts.Add(idProducto);
                            stats.Inserted++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading products & categories (ADO.NET): {ex.Message}");
            }

            return stats;
        }
    }
}
