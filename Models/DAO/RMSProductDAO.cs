using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DAO
{
    // Класс продукта
    public class RMSProductDAO
    {
        public int? Id { get; set; } // ID продукта
        public int ConsumerId { get; set; } // ID потребителя
        public ConsumerDAO Consumer { get; set; } // Потребитель
        public Guid? RMSProductExtGuid { get; set; } //UUID элемента номенклатуры
        public string Name { get; set; } = string.Empty; //Имя
        public string Description { get; set; } = string.Empty; // Описание
        public string? Num { get; set; } // Артикул, используется при печати документов (тех. карт и т.д.)
        public Guid? Parent { get; set; }
        public Guid MainUnit { get; set; } = Guid.NewGuid(); // UUID основной единицы измерения продукта
        public ItemTypeEnum Type { get; set; } = ItemTypeEnum.GOODS; // Тип элемента номенклатуры
        public List<RMSContainerDAO> Containers { get; set; } = new List<RMSContainerDAO>(); // Фасовки
        public RMSProductStatusEnum Status { get; set; } = RMSProductStatusEnum.NewProduct;

    }


    // Класс фасовки
    public class RMSContainerDAO
    {
        public int? Id { get; set; } // ID фасовки
        public Guid? RMSContainerExtGuid { get; set; } // UUID фасовки
        public int RMSProductId { get; set; } // ID продукта
        public string? Num { get; set; } // Артикул
        public string Name { get; set; } = string.Empty; // Название
        public decimal Count { get; set; } = 0.0m; // Количество продукта в единицах измерения продукта
        public decimal ContainerWeight { get; set; } = 0.0m; // Вес тары
        public decimal FullContainerWeight { get; set; } = 0.0m; // Вес элемента номенклатуры вместе с тарой
    }
}

