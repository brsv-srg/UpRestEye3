
using Google.Rpc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace UpRestEye3.Models.RMSDTO
{
    public class IntegrationUnitDTO
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string rootType { get; set; } = string.Empty;
        public bool deleted { get; set; } = false;
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;

    }

}
