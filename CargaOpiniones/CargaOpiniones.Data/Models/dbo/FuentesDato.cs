#nullable disable
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Models;

public partial class FuentesDato
{
    public string IdFuente { get; set; }

    public DateOnly FechaCarga { get; set; }

    public int? IdTipoFuente { get; set; }
}
