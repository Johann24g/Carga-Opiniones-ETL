namespace CargaOpiniones.Data
{

    public static class AppConfig
    {
        public static string ConnectionString { get; set; } =
            "Server=.;Database=AnalisisOpinionesdb;Trusted_Connection=True;TrustServerCertificate=True;";

        public static string CsvFolder { get; set; } = "Datos";

        public static string OutputFolder { get; set; } = "Salida";

        public static string Resolve(string path) =>
            System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(System.AppContext.BaseDirectory, path);
    }
}
