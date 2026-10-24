using CargaOpiniones.Data.Services;
using CargaOpiniones.Load.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CargaOpiniones.Load
{
    public class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("====================================================");
            Console.WriteLine("        INICIANDO PROCESO DE CARGA DE OPINIONES      ");
            Console.WriteLine("====================================================");

            using IHost host = AppHost.CreateHost(args);
            var service = host.Services.GetRequiredService<LoadService>();
            var result = await service.LoadOpinionsAsync();

            Console.WriteLine(result ? "Opinions loaded successfully." : "Failed to load opinions.");

            Console.WriteLine("====================================================");
            Console.WriteLine("             PROCESO FINALIZADO                     ");
            Console.WriteLine("====================================================");
        }
    }
}
