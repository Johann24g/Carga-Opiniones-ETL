using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Result;

namespace CargaOpiniones.Data.Services
{
    public class LoadService
    {
        private readonly ICategoria _categoriaLoader;
        private readonly ICliente _clienteLoader;
        private readonly IProducto _productoLoader;
        private readonly IFuenteDatos _fuenteDatosLoader;
        private readonly IOpinion _opinionLoader;

        public LoadService(
            ICategoria categoriaLoader,
            ICliente clienteLoader,
            IProducto productoLoader,
            IFuenteDatos fuenteDatosLoader,
            IOpinion opinionLoader)
        {
            _categoriaLoader = categoriaLoader;
            _clienteLoader = clienteLoader;
            _productoLoader = productoLoader;
            _fuenteDatosLoader = fuenteDatosLoader;
            _opinionLoader = opinionLoader;
        }

        public async Task<bool> LoadOpinionsAsync()
        {

            string csvFolderPath = AppConfig.Resolve(AppConfig.CsvFolder);

            string clientsPath = Path.Combine(csvFolderPath, "clients.csv");
            string productsPath = Path.Combine(csvFolderPath, "products.csv");
            string surveysPath = Path.Combine(csvFolderPath, "surveys_part1.csv");
            string socialCommentsPath = Path.Combine(csvFolderPath, "social_comments.csv");
            string webReviewsPath = Path.Combine(csvFolderPath, "web_reviews.csv");
            string fuenteDatosPath = Path.Combine(csvFolderPath, "fuente_datos.csv");

            var requiredFiles = new[] { clientsPath, productsPath, surveysPath, socialCommentsPath, webReviewsPath, fuenteDatosPath };
            foreach (var file in requiredFiles)
            {
                if (!File.Exists(file))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: no se encontró el archivo '{file}'. Revise \"Carga:CarpetaCsv\" en appsettings.json.");
                    Console.ResetColor();
                    return false;
                }
            }

            try
            {

                Console.WriteLine("Cargando Categorías...");
                var categoryResult = await _categoriaLoader.LoadCategoriaAsync(productsPath);

                Console.WriteLine("Cargando Productos...");
                var productResult = await _productoLoader.LoadProductoAsync(productsPath);

                Console.WriteLine("Cargando Clientes...");
                var clientResult = await _clienteLoader.LoadClienteAsync(clientsPath);

                Console.WriteLine("Cargando Fuentes de Datos...");
                var fuenteResult = await _fuenteDatosLoader.LoadFuenteDatosAsync(fuenteDatosPath);

                Console.WriteLine("Cargando Opiniones...");
                var opinionResult = await _opinionLoader.LoadOpinionAsync(surveysPath, socialCommentsPath, webReviewsPath);

                clientResult.Processed += opinionResult.DynamicallySeededClients;
                clientResult.Inserted += opinionResult.DynamicallySeededClients;

                productResult.Processed += opinionResult.DynamicallySeededProducts;
                productResult.Inserted += opinionResult.DynamicallySeededProducts;

                fuenteResult.Processed += opinionResult.DynamicallySeededFuentes;
                fuenteResult.Inserted += opinionResult.DynamicallySeededFuentes;

                Console.WriteLine("\n====================================================");
                Console.WriteLine("             RESUMEN DEL PROCESO DE CARGA            ");
                Console.WriteLine("====================================================");

                Console.WriteLine("Estadísticas por Servicio:");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine(string.Format("{0,-30} | {1,10} | {2,10} | {3,10}", "Entidad / Carga", "Procesados", "Insertados", "Rechazados"));
                Console.WriteLine("----------------------------------------------------------------------");

                var results = new[] { categoryResult, productResult, clientResult, fuenteResult, opinionResult };
                foreach (var res in results)
                {
                    Console.WriteLine(string.Format("{0,-30} | {1,10} | {2,10} | {3,10}",
                        res.EntityName, res.Processed, res.Inserted, res.Rejected));

                    if (res.Details != null && res.Details.Any())
                    {
                        foreach (var det in res.Details)
                        {
                            Console.WriteLine(string.Format("  - {0,-26} | {1,10} | {2,10} | {3,10}",
                                det.EntityName, det.Processed, det.Inserted, det.Rejected));
                        }
                    }
                }
                Console.WriteLine("----------------------------------------------------------------------");

                var allErrors = results.SelectMany(r => r.Errors.Select(e => new { r.EntityName, Error = e })).ToList();
                if (allErrors.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nDetalles de Validaciones / Reclamaciones ({allErrors.Count} en total):");
                    Console.ResetColor();
                    Console.WriteLine("----------------------------------------------------------------------");
                    foreach (var err in allErrors.Take(15))
                    {
                        Console.WriteLine($"[{err.EntityName}]: {err.Error}");
                    }
                    if (allErrors.Count > 15)
                    {
                        Console.WriteLine($"... y {allErrors.Count - 15} errores más.");
                    }
                }

                if (allErrors.Any())
                {
                    Console.WriteLine("\nMotivos de rechazo (agrupados):");
                    Console.WriteLine("----------------------------------------------------------------------");
                    var porMotivo = allErrors
                        .GroupBy(e => e.EntityName + " | " + Regex.Replace(Regex.Replace(e.Error, @"^.*? Row \d+: ", ""), @"\d+", "#"))
                        .OrderByDescending(g => g.Count());
                    foreach (var g in porMotivo)
                        Console.WriteLine($"{g.Count(),6} x {g.Key}");

                    string outDir = AppConfig.Resolve(AppConfig.OutputFolder);
                    Directory.CreateDirectory(outDir);
                    string rejectedPath = Path.Combine(outDir, "rechazados.csv");
                    var sb = new StringBuilder("Entidad,Detalle\r\n");
                    foreach (var er in allErrors)
                        sb.Append('"').Append(er.EntityName.Replace("\"", "\"\"")).Append("\",\"").Append(er.Error.Replace("\"", "\"\"")).Append("\"\r\n");
                    File.WriteAllText(rejectedPath, sb.ToString(), new UTF8Encoding(true));
                    Console.WriteLine($"\nSe guardó el detalle de {allErrors.Count} registros rechazados en: {rejectedPath}");
                }

                return categoryResult.Success && productResult.Success && clientResult.Success && fuenteResult.Success && opinionResult.Success;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nOcurrió un error inesperado al orquestar la carga: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalle del error interno: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                return false;
            }
        }
    }
}
