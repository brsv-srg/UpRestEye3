namespace UpRestEye3.Models.BLO
{
  
    public enum InvoiceStatusEnum
    {
        New,
        RawFile,
        QRCodeProcessed,
        TextProcessed,
        ProductsMapped,
        AttentionRequired,
        SavedToSystem,
        RecognitionError,
        UploadError
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
        FromRMS,
        NewProduct,
        NewContainer
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

