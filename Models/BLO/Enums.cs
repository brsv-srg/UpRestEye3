namespace UpRestEye3.Models.BLO
{
  
    public enum InvoiceStatusEnum
    {
        Ok,
        Error,
        Manual,
        Processing
    }

    public enum InvoiceStageEnum
    {
        New,
        QRCodeRecognition,
        TextRecognition,
        ProductsMapping,
        SavingToSystem
    }

    public enum TaxCategoryEnum
    {
        Normal,
        Intermediate,
        Reduced,
        Zero
    }
    public enum RMSProductStatusEnum
    {
        NewProduct,
        NewContainer,
        Synchronized
    }
    public enum ItemTypeEnum
    {
        GOODS,      // Товар
        DISH,       // Блюдо
        PREPARED,   // Заготовка (полуфабрикат)
        SERVICE,    // Услуга
        MODIFIER,   // Модификатор
        OUTER,      // Товары поставщиков, не являющиеся товарами систем iiko
        RATE        // Тариф (дочерний элемента для услуги)
    }


    public enum SupplierStatus
    {
        New,
        Changed,
        Synchronized
    }


    public enum EntityStatus
    {
        New,
        Changed,
        Synchronized,
        Deleted
    }
}

