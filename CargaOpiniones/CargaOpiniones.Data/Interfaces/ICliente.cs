using CargaOpiniones.Data.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Interfaces
{
    public interface ICliente
    {
        Task<OperationResult> LoadClienteAsync(string clientsFilePath);
    }
}
