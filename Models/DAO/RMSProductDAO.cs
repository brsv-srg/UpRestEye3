using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DAO
{
    // Класс продукта
    public class RMSProductDAO
    {
        public int? Id { get; set; } // ID продукта
        public int ConsumerId { get; set; } // ID потребителя
        public ConsumerDAO Consumer { get; set; } // Потребитель
        public Guid RMSProductExtGuid { get; set; }  //UUID элемента номенклатуры
        public string Name { get; set; } //Имя
        public string Description { get; set; } // Описание
        public string Num { get; set; } // Артикул, используется при печати документов (тех. карт и т.д.)
        public Guid MainUnit { get; set; } // UUID основной единицы измерения продукта
        public ItemTypeEnum Type { get; set; } // Тип элемента номенклатуры
        public List<ContainerDAO> Containers { get; set; } // Фасовки
        public RMSProductStatusEnum Status { get; set; } = RMSProductStatusEnum.NewProduct;
        public string Comments { get; set; } // Комментарий

    }


    // Класс фасовки
    public class ContainerDAO
    {
        public int? Id { get; set; } // ID фасовки
        public Guid RMSContainerExtGuid { get; set; } // UUID фасовки
        public int RMSProductId { get; set; } // ID продукта
        public string Num { get; set; } // Артикул
        public string Name { get; set; } // Название
        public decimal Count { get; set; } // Количество продукта в единицах измерения продукта
        public decimal ContainerWeight { get; set; } // Вес тары
        public decimal FullContainerWeight { get; set; } // Вес элемента номенклатуры вместе с тарой
    }
}

