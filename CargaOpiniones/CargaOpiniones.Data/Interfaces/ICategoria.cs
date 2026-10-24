using CargaOpiniones.Data.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Interfaces
{
    public interface ICategoria
    {
        Task<OperationResult> LoadCategoriaAsync(string productsFilePath);
    }
}
