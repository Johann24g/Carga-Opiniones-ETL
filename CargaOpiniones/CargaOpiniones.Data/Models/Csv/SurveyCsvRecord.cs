using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Models.Csv
{
    public class SurveyCsvRecord
    {
        public string? IdOpinion { get; set; }
        public string? IdCliente { get; set; }
        public string? IdProducto { get; set; }
        public string? Fecha { get; set; }
        public string? Comentario { get; set; }
        public string? Clasificación { get; set; }
        public string? PuntajeSatisfacción { get; set; }
        public string? Fuente { get; set; }
    }
}
