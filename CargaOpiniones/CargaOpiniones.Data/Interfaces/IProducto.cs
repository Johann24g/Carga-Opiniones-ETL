using System.Threading.Tasks;
using CargaOpiniones.Data.Result;

namespace CargaOpiniones.Data.Interfaces
{
    public interface IProducto
    {
        Task<OperationResult> LoadProductoAsync(string productsFilePath);
    }
}
