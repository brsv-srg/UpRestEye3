using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DAO
{
    // Класс продукта
    public class RMSProductDAO
    {
        public int? Id { get; set; } // ID продукта
        public int ConsumerId { get; set; } // ID потребителя
        public ConsumerDAO Consumer { get; set; } // Потребитель
        public Guid RMSProductId { get; set; }  //UUID элемента номенклатуры
        public bool Deleted { get; set; } //Удален
        public string Name { get; set; } //Имя
        public string Description { get; set; } // Описание
        public string Num { get; set; } // Артикул, используется при печати документов (тех. карт и т.д.)
        public Guid? Parent { get; set; } // UUID родительской группы продукта. Если продукт принадлежит корневой группе, то parent == null
        public Guid TaxCategory { get; set; } // UUID налоговой категории
        public Guid Category { get; set; } // UUID пользовательской категории
        public Guid AccountingCategory { get; set; } // UUID бухгалтерской категории
        public Guid MainUnit { get; set; } // UUID основной единицы измерения продукта
        public ItemType Type { get; set; } // Тип элемента номенклатуры
        public decimal UnitWeight { get; set; } // Вес одной единицы в килограммах
        public decimal UnitCapacity { get; set; } // Объем одной единицы в литрах
        public bool NotInStoreMovement { get; set; } // Участвует ли в перемещениях по складу
        public List<ContainerDAO> Containers { get; set; } // Фасовки
    }


    // Класс фасовки
    public class ContainerDAO
    {
        public int? Id { get; set; } // ID фасовки
        public Guid RMSContainerId { get; set; } // UUID фасовки
        public string Num { get; set; } // Артикул
        public string Name { get; set; } // Название
        public decimal Count { get; set; } // Количество продукта в единицах измерения продукта
        public decimal MinContainerWeight { get; set; } // Минимальный вес элемента номенклатуры
        public decimal MaxContainerWeight { get; set; } // Максимальный вес элемента номенклатуры
        public decimal ContainerWeight { get; set; } // Вес тары
        public decimal FullContainerWeight { get; set; } // Вес элемента номенклатуры вместе с тарой
        public bool BackwardRecalculation { get; set; } // Всегда false
        public bool UseInFront { get; set; } // Использовать на фронте
        public bool Deleted { get; set; } // Удалена или нет
    }
}

