using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Services
{
    public class ClienteServiceAdo : ICliente
    {
        private readonly string _connectionString = AppConfig.ConnectionString;

        public async Task<OperationResult> LoadClienteAsync(string clientsFilePath)
        {
            var stats = new OperationResult { EntityName = "Clientes (ADO.NET)", Success = true };

            try
            {
                if (!File.Exists(clientsFilePath))
                {
                    stats.Rejected++;
                    stats.Errors.Add($"File '{clientsFilePath}' not found.");
                    return stats;
                }

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower()
                };

                var existingClients = new HashSet<int>();
                var existingEmails = new HashSet<string>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SELECT IdCliente, Email FROM Clientes", connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingClients.Add(reader.GetInt32(0));
                            string email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                existingEmails.Add(email.Trim().ToLower());
                            }
                        }
                    }
                }

                using (var reader = new StreamReader(clientsFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<ClientCsvRecord>().ToList();

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        foreach (var record in records)
                        {
                            stats.Processed++;

                            if (string.IsNullOrWhiteSpace(record.IdCliente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: IdCliente is required.");
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(record.Nombre))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Nombre is required.");
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(record.Email))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Email is required.");
                                continue;
                            }

                            string cleanIdStr = Regex.Replace(record.IdCliente, @"[^\d]", "");
                            if (!int.TryParse(cleanIdStr, out int idCliente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Invalid IdCliente format '{record.IdCliente}'.");
                                continue;
                            }

                            string email = record.Email.Trim().ToLower();
                            if (!email.Contains("@") || !email.Contains("."))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Invalid Email format '{record.Email}'.");
                                continue;
                            }

                            if (existingClients.Contains(idCliente))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Duplicate IdCliente {idCliente} already exists.");
                                continue;
                            }

                            if (existingEmails.Contains(email))
                            {
                                stats.Rejected++;
                                stats.Errors.Add($"Row {stats.Processed}: Duplicate Email '{email}' already exists.");
                                continue;
                            }

                            using (var insertCmd = new SqlCommand("INSERT INTO Clientes (IdCliente, Nombre, Email) VALUES (@Id, @Nombre, @Email)", connection))
                            {
                                insertCmd.Parameters.AddWithValue("@Id", idCliente);
                                insertCmd.Parameters.AddWithValue("@Nombre", record.Nombre.Trim());
                                insertCmd.Parameters.AddWithValue("@Email", email);
                                await insertCmd.ExecuteNonQueryAsync();
                            }

                            existingClients.Add(idCliente);
                            existingEmails.Add(email);
                            stats.Inserted++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading clients (ADO.NET): {ex.Message}");
            }

            return stats;
        }
    }
}
