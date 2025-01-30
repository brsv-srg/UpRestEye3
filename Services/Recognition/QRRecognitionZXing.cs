using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Runtime.Versioning;
using UpRestEye3.Models.BLO;


namespace UpRestEye3.Services.Recognition
{


    public class QRRecognitionZXing : IQRRecognition
    {
        private readonly List<BarcodeReader> _readers;

        public QRRecognitionZXing()
        {
            _readers = CreateReaders();
        }

        public QRCodeData DecodeQRCode(Bitmap sourceImage)
        {
            // Конвертация в Bitmap для ZXing
            // Попытка распознать 
            foreach (var barcodeReader in _readers)
            {
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
                else
                {
                    var resultSimple = barcodeReader.Decode(sourceImage);
                    if (resultSimple != null && QRCodeData.IsMatchingATQRCode(resultSimple.Text))
                    {
                        // Успешно распознан QR-код с нужной структурой
                        return new QRCodeData(resultSimple.Text);
                    }
                }
            }

            return null;
        }

        private List<BarcodeReader> CreateReaders()
        {
            var readers = new List<BarcodeReader>();
            readers.Add(new BarcodeReader
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            readers.Add(new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            readers.Add(new BarcodeReader
            {
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            readers.Add(new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            readers.Add(new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            readers.Add(new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    PureBarcode = false
                }
            });
            return readers;
        }
    }
}
