using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Models;
using CargaOpiniones.Data.Models.Csv;
using CargaOpiniones.Data.Result;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Services
{
    public class ClienteService : ICliente
    {
        private readonly AnalisisOpinionesdbContext _context;

        public ClienteService(AnalisisOpinionesdbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult> LoadClienteAsync(string clientsFilePath)
        {
            var stats = new OperationResult { EntityName = "Clientes", Success = true };

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

                var existingClients = await _context.Clientes.ToDictionaryAsync(c => c.IdCliente);
                var existingEmails = new HashSet<string>(await _context.Clientes.Select(c => c.Email.ToLower()).ToListAsync());

                using (var reader = new StreamReader(clientsFilePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    var records = csv.GetRecords<ClientCsvRecord>().ToList();
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

                        if (existingClients.ContainsKey(idCliente))
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

                        var nuevoCliente = new Cliente
                        {
                            IdCliente = idCliente,
                            Nombre = record.Nombre.Trim(),
                            Email = email
                        };

                        _context.Clientes.Add(nuevoCliente);
                        existingClients.Add(idCliente, nuevoCliente);
                        existingEmails.Add(email);
                        stats.Inserted++;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Errors.Add($"Error loading clients: {ex.Message}");
            }

            return stats;
        }
    }
}
