using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Models.Csv
{
    public class FuenteDatosCsvRecord
    {
        public string? IdFuente { get; set; }
        public string? TipoFuente { get; set; }
        public string? FechaCarga { get; set; }
    }
}
