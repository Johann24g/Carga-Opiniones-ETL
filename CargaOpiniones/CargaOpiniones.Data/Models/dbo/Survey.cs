#nullable disable
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Models;

public partial class Survey
{
    public int IdOpinion { get; set; }

    public int? PuntajeSatisfaccion { get; set; }

    public int IdClasificacion { get; set; }
}
