using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using CargaOpiniones.Data;
using CargaOpiniones.Data.Context;
using CargaOpiniones.Data.Interfaces;
using CargaOpiniones.Data.Services;

namespace CargaOpiniones.Load.Host
{
    public static class AppHost
    {
        public static IHost CreateHost(string[] args)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            AppConfig.ConnectionString = config.GetConnectionString("AnalisisOpiniones") ?? AppConfig.ConnectionString;
            AppConfig.CsvFolder = config["Carga:CarpetaCsv"] ?? AppConfig.CsvFolder;
            AppConfig.OutputFolder = config["Carga:CarpetaSalida"] ?? AppConfig.OutputFolder;

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AnalisisOpinionesdbContext>(options =>
                        options.UseSqlServer(AppConfig.ConnectionString));

                    services.AddTransient<ICategoria, CategoriaService>();
                    services.AddTransient<ICliente, ClienteService>();
                    services.AddTransient<IProducto, ProductoService>();
                    services.AddTransient<IFuenteDatos, FuenteDatosService>();
                    services.AddTransient<IOpinion, OpinionService>();

                    services.AddTransient<LoadService>();
                })
                .Build();
        }
    }
}
