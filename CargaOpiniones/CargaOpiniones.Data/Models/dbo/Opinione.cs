#nullable disable
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Models;

public partial class Opinione
{
    public int IdOpinion { get; set; }

    public int? IdCliente { get; set; }

    public int IdProducto { get; set; }

    public string IdFuente { get; set; }

    public DateOnly Fecha { get; set; }

    public string Comentario { get; set; }
}
