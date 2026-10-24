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
using Microsoft.Data.SqlClient;

namespace CargaOpiniones.Data.Services
{
    public class CategoriaServiceAdo : ICategoria
    {
        private readonly string _connectionString = AppConfig.ConnectionString;

        public async Task<OperationResult> LoadCategoriaAsync(string productsFilePath)
        {
            var stats = new OperationResult { EntityName = "Categorias (ADO.NET)", Success = true };

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
                                stats.Processed++;
                                int newId = existingCategorias.Count > 0 ? existingCategorias.Values.Max() + 1 : 1;

                                using (var insertCommand = new SqlCommand("INSERT INTO Categorias (IdCategoria, NombreCategoria) VALUES (@Id, @Name)", connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@Id", newId);
                                    insertCommand.Parameters.AddWithValue("@Name", catName);
                                    await insertCommand.ExecuteNonQueryAsync();
                                }

                                existingCategorias[catKey] = newId;
                                stats.Inserted++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading categories (ADO.NET): {ex.Message}");
            }

            return stats;
        }
    }
}
