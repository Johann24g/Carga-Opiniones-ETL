using System.Collections.Generic;

namespace CargaOpiniones.Data.Result
{
    public class OperationResult
    {
        public string EntityName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Processed { get; set; }
        public int Inserted { get; set; }
        public int Rejected { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<OperationResult> Details { get; set; } = new List<OperationResult>();

        public int DynamicallySeededClients { get; set; }
        public int DynamicallySeededProducts { get; set; }
        public int DynamicallySeededFuentes { get; set; }
    }
}
