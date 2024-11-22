using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UpRestEye3.Models;
using System.Runtime.Versioning;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Http;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;
using ZXing.QrCode.Internal;
using Microsoft.AspNetCore.Http.HttpResults;



public class FileProcessor
{
    [SupportedOSPlatform("windows")]
    public async Task<Invoice> ProcessFileAsync(string filePath)
    {
        // Load the image from the file
        using (var originalBitmap = new Bitmap(filePath))
        {
            // Предварительная обработка изображения
            var grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            var contrastFilter = new ContrastCorrection(50);
            var binaryFilter = new Threshold(100);

            Bitmap preprocessedBitmap = binaryFilter.Apply(contrastFilter.Apply(grayFilter.Apply(originalBitmap)));

            var reader = new BarcodeReader<Bitmap>(bitmap => new BitmapLuminanceSource(preprocessedBitmap));

            reader.Options.PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE };
            reader.AutoRotate = true;
            reader.Options.TryHarder = true;
            reader.Options.PureBarcode = false;

            // Decode the QR code from the image
            var results = reader.DecodeMultiple(preprocessedBitmap);

            if (results != null && results.Length > 0)
            {
                // Assuming the QR code contains JSON data for the Invoice
                var invoiceData = new QRCodeData(results.Last().Text);

                // Deserialize the JSON data to an Invoice object
                var invoice = new Invoice (invoiceData);

                return invoice;
            }
        }

        return null;
    }

    /*
    [SupportedOSPlatform("windows")]
    public async Task<string>TestQRCodeTechnologyAsync(string invoiceJson, string filePath)
    {
        // Generate QR code
        var writer = new BarcodeWriter<Bitmap>
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 300,
                Width = 300
            },
            Renderer = new BitmapRenderer()
        };

        using (var bitmap = writer.Write(invoiceJson))
        {
            // Save QR code to file
            bitmap.Save(filePath, ImageFormat.Png);

            // Read and decode the QR code from the file
            var invoice = await ProcessFileAsync(filePath);

            // Output the result (for testing purposes)
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(invoice));
        }
        return null;
    }
    */
}
