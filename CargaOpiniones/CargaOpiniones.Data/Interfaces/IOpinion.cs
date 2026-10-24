using CargaOpiniones.Data.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargaOpiniones.Data.Interfaces
{
    public interface IOpinion
    {
        Task<OperationResult> LoadOpinionAsync(string surveysPath, string socialCommentsPath, string webReviewsPath);
    }
}
