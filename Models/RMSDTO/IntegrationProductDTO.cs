
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace UpRestEye3.Models.RMSDTO
{
    public class GetProductDTO
    {
        public Guid? id { get; set; }
        public bool deleted { get; set; } = false;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public string? num { get; set; }
        public Guid? parent { get; set; }
        public Guid mainUnit { get; set; } = Guid.Empty;
        public string type { get; set; } = "GOODS";
        public decimal unitWeight { get; set; } = 0;
        public decimal unitCapacity { get; set; } = 0;
        public bool notInStoreMovement { get; set; } = false;
        public List<GetContainerDTO> containers { get; set; } = new List<GetContainerDTO>();
    }

    public class GetContainerDTO
    {
        public Guid? id { get; set; }
        public string name { get; set; } = "";
        public string? num { get; set; }
        public decimal count { get; set; } = 0;
        public decimal minContainerWeight { get; set; } = 0;
        public decimal maxContainerWeight { get; set; } = 0;
        public decimal containerWeight { get; set; } = 0;
        public decimal fullContainerWeight { get; set; } = 0;

    }






    public class SaveProductDTO
    {
        public string name { get; set; } = string.Empty;
        public string? description { get; set; } = string.Empty;
        public string? num { get; set; }
        public string code { get; set; } = string.Empty;
        public Guid? parent { get; set; }
        public string[]? modifiers { get; set; }
        public string? taxCategory { get; set; }
        public string? category { get; set; }
        public string? accountingCategory { get; set; }
        public string? color { get; set; }
        public string? fontColor { get; set; }
        public string? frontImageId { get; set; }
        public string? position { get; set; }
        public Guid mainUnit { get; set; } = Guid.NewGuid();
        public string? excludedSections { get; set; }
        public decimal? defaultSalePrice { get; set; }
        public string? placeType { get; set; }
        public bool? defaultIncludedInMenu { get; set; }
        public string type { get; set; } = "GOODS";
        public decimal unitWeight { get; set; } = 1;
        public decimal unitCapacity { get; set; } = 0;
        public bool? notInStoreMovement { get; set; }
        public List<SaveContainerDTO> containers { get; set; } = new List<SaveContainerDTO>();
        public decimal coldLossPercent { get; set; } = 0;
        public decimal hotLossPercent { get; set; } = 0;
        public string? allergenGroups { get; set; }
        public decimal? estimatedPurchasePrice { get; set; }
    }

    public class SaveContainerDTO
    {
        public string name { get; set; } = string.Empty;
        public string num { get; set; } = string.Empty;
        public decimal count { get; set; } = 0;
        public decimal minContainerWeight { get; set; } = 0;
        public decimal containerWeight { get; set; } = 0;
        public decimal fullContainerWeight { get; set; } = 0;
        public bool? backwardRecalculation { get; set; }
        public bool? useInFront { get; set; }
    }




    public class SaveProductResponse
    {
        public string result { get; set; }
        public List<ErrorDto> errors { get; set; }
        public GetProductDTO response { get; set; }
    }

    public class ErrorDto
    {
        public string value { get; set; }
        public string code { get; set; }
    }


}
