#nullable disable
using System;
using System.Collections.Generic;

namespace CargaOpiniones.Data.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; }

    public string Email { get; set; }
}
