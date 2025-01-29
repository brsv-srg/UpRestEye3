
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace UpRestEye3.Models.DTO
{
    public class ProductDTO
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public bool deleted { get; set; } = false;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public string num { get; set; } = "";
        public Guid? parent { get; set; } = Guid.Empty;
        public Guid mainUnit { get; set; } = Guid.Empty;
        public string type { get; set; } =  string.Empty;
        public decimal unitWeight { get; set; } = 0;
        public decimal unitCapacity { get; set; } = 0;
        public bool notInStoreMovement { get; set; } = false;
        public List<ContainerDTO> containers { get; set; } = new List<ContainerDTO>();
    }

    public class ContainerDTO
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string num { get; set; } = "";
        public string name { get; set; } = "";
        public decimal count { get; set; } = 0;
        public decimal minContainerWeight { get; set; } = 0;
        public decimal maxContainerWeight { get; set; } = 0;
        public decimal containerWeight { get; set; } = 0;
        public decimal fullContainerWeight { get; set; } = 0;

    }
}
