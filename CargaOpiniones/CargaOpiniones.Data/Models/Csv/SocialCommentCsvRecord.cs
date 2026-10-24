using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Models.Csv
{
    public class SocialCommentCsvRecord
    {
        public string? IdComment { get; set; }
        public string? IdCliente { get; set; }
        public string? IdProducto { get; set; }
        public string? Fuente { get; set; }
        public string? Fecha { get; set; }
        public string? Comentario { get; set; }
    }

}
