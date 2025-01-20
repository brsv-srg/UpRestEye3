namespace UpRestEye3.Models.BLO
{
  
    public enum InvoiceStatus
    {
        New,
        RawFile,
        QRCodeProcessed,
        TextProcessed,
        ProductsMapped,
        SavedToSystem,
        Error
    }

    public enum TaxCategory
    {
        Normal,
        Intermediate,
        Reduced,
        Zero
    }
}

