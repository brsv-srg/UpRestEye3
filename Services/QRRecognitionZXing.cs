using System.Drawing;
using ZXing;
using ZXing.Common;
using UpRestEye3.Models;
using ZXing.Windows.Compatibility;
using OpenCvSharp.Extensions;
using OpenCvSharp;


namespace UpRestEye3.Services
{ 
public class QRRecognitionZXing: IQRRecognition
    {
        // TODO распознавание разными способами
        public QRCodeData DecodeQRCode(Bitmap sourceImage)
        {
            // Распознавание ZXing
            // Настройка 
            var barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            };

            // Конвертация в Bitmap для ZXing
            // Попытка распознать 
            var results = barcodeReader.DecodeMultiple(sourceImage);
            if (results != null && results.Length > 0)
            {
                // Успешно распознаны
                foreach (var result in results)
                {
                    if (result != null && QRCodeData.IsMatchingATQRCode(result.Text))
                    {
                        // Успешно распознан QR-код с нужной структурой
                        return new QRCodeData(result.Text);
                    }
                }
            }
            return null;
        }
    }
}
