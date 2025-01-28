namespace UpRestEye3.Models.BLO
{
    // Тип элемента номенклатуры
    public enum ItemType
    {
        GOODS,      // Товар
        DISH,       // Блюдо
        PREPARED,   // Заготовка (полуфабрикат)
        SERVICE,    // Услуга
        MODIFIER,   // Модификатор
        OUTER,      // Товары поставщиков, не являющиеся товарами систем iiko
        RATE        // Тариф (дочерний элемента для услуги)
    }

}

