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


namespace UpRestEye3.Services
{ 
    public class FileProcessor
    {
        [SupportedOSPlatform("windows")]
        public async Task<QRCodeData> ProcessFileAsync(string filePath)
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
                    var qrInvoiceData = new QRCodeData(results.Last().Text);
                    return qrInvoiceData;
                }
            }
            return null;
        }
    }
}
