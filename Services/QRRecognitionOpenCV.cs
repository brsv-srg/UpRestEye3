using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using UpRestEye3.Models;


namespace UpRestEye3.Services
{ 
    public interface IQRRecognition
    {
        QRCodeData DecodeQRCode(Bitmap sourceImage);
    }
    
    public class QRRecognitionOpenCV: IQRRecognition
    {

        public QRCodeData DecodeQRCode(Bitmap sourceImage)
        {
            var detector = new QRCodeDetector();
            var matImage = BitmapConverter.ToMat(sourceImage);

            bool isDetected = detector.DetectMulti(matImage, out Point2f[] points);
            if (!isDetected)
                return null;

            isDetected = detector.DecodeMulti(matImage, points, out string?[] results);
            if (isDetected)
            {
                // Успешно распознаны
                foreach (var result in results)
                {
                    if (result != null && QRCodeData.IsMatchingATQRCode(result))
                    {
                        // Успешно распознан QR-код с нужной структурой
                        return new QRCodeData(result);
                    }
                }
            }
            return null;
        }
    }
}
