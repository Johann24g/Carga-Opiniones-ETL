#nullable disable
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string NombreProducto { get; set; }

    public int IdCategoria { get; set; }
}
