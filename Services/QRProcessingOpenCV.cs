using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UpRestEye3.Models;
using System.Runtime.Versioning;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Http;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;
using ZXing.QrCode.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using ZXing.Common;
using System.Text.RegularExpressions;
using System.Reflection.PortableExecutable;
using OpenCvSharp.XImgProc;
using ImageMagick;
using AForge.Imaging;
using UpRestEye3.MLImageModels;
using Google.Api;
using static UpRestEye3.Services.LocalMLService;
using System.Runtime.InteropServices;
using AForge.Math;
using Google.LongRunning;
using NumSharp;
using System.Diagnostics.Metrics;
using Tensorflow.Keras.Layers;
using Tensorflow.Operations.Initializers;


namespace UpRestEye3.Services
{
    public interface IQRProcessing
    {
        Task<Bitmap> GrayScale(Bitmap sourceImage);
        ImageDigest GenerateImageDigest(Bitmap sourceImage);
        Bitmap ApplyImageProcessing(Bitmap sourceImage, string imagePath, ImageProcessingParameters parameters);
        Bitmap ApplyImageProcessing2(Bitmap sourceImage, string imagePath, ImageProcessingParameters parameters);


    }

    public class QRProcessingOpenCV : IQRProcessing
    {

        public async Task<Bitmap> GrayScale(Bitmap sourceImage)
        {
            // Преобразование изображения в оттенки серого с использованием OpenCV
            using (Mat mat = BitmapConverter.ToMat(sourceImage))
            {
                using Mat grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
                return BitmapConverter.ToBitmap(grayMat);
            }
        }

        private async Task<Bitmap> GrayScale2(Bitmap sourceImage)
        {
            // Преобразование изображения в оттенки серого
            var grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            return grayFilter.Apply(sourceImage);
        }

        public ImageDigest GenerateImageDigest(Bitmap sourceImage)
        {
            using Mat matImage = BitmapConverter.ToMat(sourceImage);

            // Оценка яркости
            var brightness = Cv2.Mean(matImage).Val0;

            // Оценка контраста
            Cv2.MeanStdDev(matImage, out _, out var stdDev);
            var contrast = stdDev.Val0;

            // Оценка резкости - вариант 1 (исходный)
            //using Mat laplacian = new Mat();
            //Cv2.Laplacian(matImage, laplacian, MatType.CV_64F);
            //Scalar mu, sigma;
            //Cv2.MeanStdDev(laplacian, out mu, out sigma);
            //double sharpness = sigma.Val0 * sigma.Val0;

            // Оценка размытости - вариант 2 (от клауде)
            using Mat laplacian = new Mat();
            Cv2.Laplacian(matImage, laplacian, MatType.CV_64F);
            using Mat absoluteLaplacian = new Mat();
            Cv2.ConvertScaleAbs(laplacian, absoluteLaplacian);
            var sharpness = Cv2.Mean(absoluteLaplacian).Val0;

            // Оценка уровня шума - вариант 1 (исходный)
            //using Mat blurredImage = new Mat();
            //Cv2.GaussianBlur(matImage, blurredImage, new OpenCvSharp.Size(5, 5), 0);
            //var noiseLevel = Cv2.Mean(blurredImage).Val0;

            // Оценка уровня шума - вариант 2 (от клауде)
            using Mat noise = new Mat();
            Cv2.FastNlMeansDenoising(matImage, noise);
            using Mat diff = new Mat();
            Cv2.Subtract(matImage, noise, diff);
            var noiseLevel = Cv2.Mean(diff).Val0;


            int width = matImage.Width;
            int height = matImage.Height;
            double aspectRatio = (double)width / height;

            return new ImageDigest
            {
                brightness = (float)brightness,
                contrast = (float)contrast,
                noiseLevel = (float)noiseLevel,
                sharpness = (float)sharpness,
                aspectRatio = (float)aspectRatio
            };
        }




        public Bitmap ApplyImageProcessing(Bitmap sourceImage, string imagePath, ImageProcessingParameters parameters)
        {
            using Mat matImage = BitmapConverter.ToMat(sourceImage);

            Mat resultImage = matImage;
            // 2. Преобразование в оттенки серого
            //using Mat gray = new Mat();
            //Cv2.CvtColor(resultImage, gray, ColorConversionCodes.BGR2GRAY); //+
            //resultImage = gray;

            // 3. Удаление шума
            if (parameters.medianBlurKernel > 0)
            {
                using Mat denoised = new Mat();
                Cv2.MedianBlur(resultImage, denoised, (int)parameters.medianBlurKernel); //+ 
                resultImage = denoised;

                {// запись
                    using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp1-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}{Path.GetExtension(imagePath)}");


                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }

            // 4. Усиление контраста
            if (parameters.convScaleContrast != 0.0 || parameters.convScaleBrightness != 0.0)
            {
                using Mat enhanced = new Mat();
                Cv2.ConvertScaleAbs(resultImage, enhanced, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость
                resultImage = enhanced;


                {// запись
                    using var bitmap = BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp2-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}{Path.GetExtension(imagePath)}");


                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }


            // 5. Бинаризация
            if (parameters.adThreshBlock > 0)
            {
                using Mat binary = new Mat();
                Cv2.Threshold(resultImage, binary, parameters.adThreshBlock, 255, ThresholdTypes.Otsu);
                //Cv2.AdaptiveThreshold(resultImage, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, (int)parameters.adThreshBlock, 2);
                resultImage = binary;


                {// запись
                    using Bitmap bitmap = BitmapConverter.ToBitmap(resultImage);
                    string processedFilePath = Path.Combine(Path.GetDirectoryName(imagePath), "processed", $"temp3-{Path.GetFileNameWithoutExtension(imagePath)}-{parameters.medianBlurKernel}-{parameters.convScaleContrast}-{parameters.convScaleBrightness}-{parameters.adThreshBlock}{Path.GetExtension(imagePath)}");

                    Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
                    bitmap.Save(processedFilePath, ImageFormat.Png);
                }
            }
            //
            return BitmapConverter.ToBitmap(resultImage);
        }

        public Bitmap ApplyImageProcessing2(Bitmap sourceImage, string imagePath, ImageProcessingParameters parameters)
        {
            using Mat matImage = BitmapConverter.ToMat(sourceImage);
            Mat resultImage = matImage;

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///*****************************************************************************************************************************///
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Cv2.Resize(resultImage, resultImage, new OpenCvSharp.Size(0, 0), fx: 1.2, fy: 1.2, interpolation: InterpolationFlags.Cubic);
            SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-test-Resize");

            Cv2.Dilate(resultImage, resultImage, new Mat(), new OpenCvSharp.Point(-1, -1), iterations: 1);
            SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-test-Dilate");

            Cv2.Erode(resultImage, resultImage, new Mat(), new OpenCvSharp.Point(-1, -1), iterations: 1);
            SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-test-Erode");

            {
                Mat mat1 = new Mat();
                Cv2.GaussianBlur(resultImage, mat1, new OpenCvSharp.Size(5, 5), 0);
                SaveImage(BitmapConverter.ToBitmap(mat1), imagePath, $@"-test11-GaussianBlur");
                Cv2.Threshold(mat1, mat1, 0, 255, ThresholdTypes.Otsu);
                SaveImage(BitmapConverter.ToBitmap(mat1), imagePath, $@"-test11-Threshold");
            }

            {
                Mat mat2 = new Mat();
                Cv2.BilateralFilter(resultImage, mat2, 5, 75,75);
                SaveImage(BitmapConverter.ToBitmap(mat2), imagePath, $@"-test12-BilateralFilter");
                Cv2.Threshold(mat2, mat2, 0, 255, ThresholdTypes.Otsu);
                SaveImage(BitmapConverter.ToBitmap(mat2), imagePath, $@"-test12-Threshold");
            }

            {
                Mat mat3 = new Mat();
                Cv2.MedianBlur(resultImage, mat3, 3);
                SaveImage(BitmapConverter.ToBitmap(mat3), imagePath, $@"-test13-MedianBlur");
                Cv2.Threshold(mat3, mat3, 0, 255, ThresholdTypes.Otsu);
                SaveImage(BitmapConverter.ToBitmap(mat3), imagePath, $@"-test13-Threshold");
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////
            {
                Mat mat1 = new Mat();
                Cv2.GaussianBlur(resultImage, mat1, new OpenCvSharp.Size(5, 5), 0);
                SaveImage(BitmapConverter.ToBitmap(mat1), imagePath, $@"-test21-GaussianBlur");
                Cv2.AdaptiveThreshold(mat1, mat1, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 2);
                SaveImage(BitmapConverter.ToBitmap(mat1), imagePath, $@"-test21-AdaptiveThreshold");
            }

            {
                Mat mat2 = new Mat();
                Cv2.BilateralFilter(resultImage, mat2, 5, 75, 75);
                SaveImage(BitmapConverter.ToBitmap(mat2), imagePath, $@"-test2-BilateralFilter");
                Cv2.AdaptiveThreshold(mat2, mat2, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 2);
                SaveImage(BitmapConverter.ToBitmap(mat2), imagePath, $@"-test22-AdaptiveThreshold");
            }

            {
                Mat mat3 = new Mat();
                Cv2.MedianBlur(resultImage, mat3, 3);
                SaveImage(BitmapConverter.ToBitmap(mat3), imagePath, $@"-test3-MedianBlur");
                Cv2.AdaptiveThreshold(mat3, mat3, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 2);
                SaveImage(BitmapConverter.ToBitmap(mat3), imagePath, $@"-test23-AdaptiveThreshold");
            }

            return BitmapConverter.ToBitmap(resultImage);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///*****************************************************************************************************************************///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



            //  Усиление яркости и контраста
            if (parameters.convScaleContrast != 1.0 || parameters.convScaleBrightness != 0.0)
            {
                Mat enhanced = new Mat();
                Cv2.ConvertScaleAbs(resultImage, enhanced, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость
                resultImage = enhanced;


                SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-bright");
            }

            // Удаление шума
            if (parameters.medianBlurKernel > 0)
            {
                Mat denoised = new Mat();
                Cv2.FastNlMeansDenoising(resultImage, denoised, (int)parameters.medianBlurKernel); //+ 
                resultImage = denoised;

                SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-noise");
            }

            // Повышение резкости 
            if (parameters.sharpLevel > 0)
            {
                float centerValue = 9.0f;    // (float) parameters.sharpLevel;
                float surroundValue = -1;   // -centerValue / 8.0f;

                using var kernel = new Mat();
                float[,] data = new float[,]
                {
                    { surroundValue, surroundValue, surroundValue },
                    { surroundValue,  centerValue,  surroundValue },
                    { surroundValue, surroundValue, surroundValue }
                };
                kernel.Create(new OpenCvSharp.Size(3,3), MatType.CV_32F);
                Marshal.Copy(data.Cast<float>().ToArray(), 0, kernel.Data, 9);


                Mat sharp = new Mat();
                Cv2.Filter2D(resultImage, sharp, -1, kernel, new OpenCvSharp.Point(-1, -1));
                resultImage = sharp;

                SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-sharp");
            }



            // Адаптивная бинаризация
            if (parameters.adThreshBlock > 0)
            {

                Mat thresholded = new Mat();
                
                Cv2.GaussianBlur(resultImage, resultImage, new OpenCvSharp.Size(5, 5), 0);

                Cv2.Threshold(resultImage, thresholded, parameters.adThreshBlock, 255, ThresholdTypes.Otsu);

                /*Cv2.AdaptiveThreshold(resultImage, thresholded, 255,
                                        AdaptiveThresholdTypes.GaussianC,
                                        ThresholdTypes.Binary,
                                        (int)parameters.adThreshBlock,
                                        (int)parameters.adThreshC);
                                        */
                resultImage = thresholded;

                SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-thresh");
            }


            // Морфологические операции
            if (parameters.medianBlurKernel > 0 || parameters.sharpLevel > 0)
            {
                using Mat kernel = Cv2.GetStructuringElement(
                    MorphShapes.Rect,
                    new OpenCvSharp.Size(3, 3));
                Mat morphologed = new Mat();

                if (parameters.medianBlurKernel > 15 || parameters.sharpLevel > 40)
                {
                    Cv2.MorphologyEx(
                        resultImage,
                        morphologed,
                        MorphTypes.Close,
                        kernel);
                    resultImage = morphologed;
                }

                Cv2.MorphologyEx(
                    resultImage,
                    morphologed,
                    MorphTypes.Gradient,
                    kernel);
                resultImage = morphologed;

                SaveImage(BitmapConverter.ToBitmap(resultImage), imagePath, $@"-morf");
            }

            return BitmapConverter.ToBitmap(resultImage);
        }



        public void SaveImage(Bitmap image, string imagePath, string nameModif)
        {// запись
            
            string processedFilePath = Path.Combine(
                                        Path.GetDirectoryName(imagePath), 
                                        "processed", 
                                        $"{Path.GetFileNameWithoutExtension(imagePath)}-{nameModif}{Path.GetExtension(imagePath)}");

            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            image.Save(processedFilePath, ImageFormat.Png);
        }
}
}
