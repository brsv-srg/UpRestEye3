using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Services.Recognition   
{
    public interface IQRProcessing
    {
        Task<Bitmap> GrayScale(Bitmap sourceImage);
        ImageDigest GenerateImageDigest(Bitmap sourceImage);
        Bitmap ApplyImageProcessing(Bitmap sourceImage, ImageProcessingPipeline pipeline, string imagePath, string callStack = "");


    }

    public class QRProcessingOpenCV : IQRProcessing
    {

        public async Task<Bitmap> GrayScale(Bitmap sourceImage)
        {
            // Преобразование изображения в оттенки серого с использованием OpenCV
            using (Mat mat = sourceImage.ToMat())
            {
                using Mat grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
                return grayMat.ToBitmap();
            }
        }

       
        public ImageDigest GenerateImageDigest(Bitmap sourceImage)
        {
            using Mat matImage = sourceImage.ToMat();

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


            var resDigest = new ImageDigest
            {
                brightness = (float)brightness,
                contrast = (float)contrast,
                noiseLevel = (float)noiseLevel,
                sharpness = (float)sharpness,
                aspectRatio = (float)aspectRatio
            };
            InitImageType(resDigest);
            return resDigest;
        }

        // TODO поправить классификацию
        private void InitImageType(ImageDigest digest)
        {
            // TODO проверить классификацию, поправить критерии
            // Классификация изображения на основе метрик

            // Очистка флагов
            digest.imageType = 0;

            // Темное / светлое
            if (digest.brightness < 200)
                digest.imageType |= ImageDigest.ImageType.Dark;
            else
                digest.imageType |= ImageDigest.ImageType.Light;

            // Контрастное / не контрастное
            if (digest.contrast < 30) //50
                digest.imageType |= ImageDigest.ImageType.UnContrast;
            else
                digest.imageType |= ImageDigest.ImageType.Contrast;

            // Не в фокусе / в фокусе 
            if (digest.sharpness < 10)
                digest.imageType |= ImageDigest.ImageType.Blurred;
            else
                digest.imageType |= ImageDigest.ImageType.Focused;

            // Зашумленное / чистое
            if (digest.noiseLevel > 10) // 1
                digest.imageType |= ImageDigest.ImageType.Noisy;
            else
                digest.imageType |= ImageDigest.ImageType.Clean;

        }


        public Bitmap ApplyImageProcessing(Bitmap sourceImage, ImageProcessingPipeline pipeline, string imagePath, string callStack = "")
        {
            using Mat matImage = sourceImage.ToMat();
            Mat processedImage = matImage;

            try
            {
                processedImage = ApplyGrayscale(processedImage, pipeline.Grayscale, imagePath, callStack + "-gray");
                processedImage = ApplyResize(processedImage, pipeline.Resize, imagePath, callStack + "-resize");
                processedImage = ApplyContrastBrightnessAdjustment(processedImage, pipeline.ContrastBrightnessAdjustment, imagePath, callStack + "-contrBright");
                processedImage = ApplyNoiseRemoval(processedImage, pipeline.NoiseRemoval, imagePath, callStack + "-noiseRem");
                processedImage = ApplySharpening(processedImage, pipeline.Sharpening, imagePath, callStack + "-shape");
                processedImage = ApplyBinarization(processedImage, pipeline.Binarization, imagePath, callStack + "-binar");
                processedImage = ApplyNoiseRemoval(processedImage, pipeline.FinalNoiseRemoval, imagePath, callStack + "-finNoiseRem");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return matImage.ToBitmap();
        }


        private Mat ApplyGrayscale(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute && stage.FunctionName == "CvtColor")
            {
                Cv2.CvtColor(image, image, (ColorConversionCodes)stage.Parameters["code"]);
                SaveImage(image.ToBitmap(), imagePath, @$"CvtColor-{stage.Parameters["code"]}", callStack);
            }
            return image;
        }

        private Mat ApplyResize(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute && stage.FunctionName == "Resize")
            {
                Cv2.Resize(image, image, new OpenCvSharp.Size(0, 0),
                    fx: stage.Parameters["fx"], fy: stage.Parameters["fy"],
                    interpolation: InterpolationFlags.Cubic);
                SaveImage(image.ToBitmap(), imagePath, @$"Resize-{stage.Parameters["fx"]}-{stage.Parameters["fy"]}", callStack);
            }
            return image;
        }

        private Mat ApplyNoiseRemoval(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute)
            {
                if (stage.FunctionName == "GaussianBlur")
                {
                    Cv2.GaussianBlur(image, image, new OpenCvSharp.Size(stage.Parameters["ksize"], stage.Parameters["ksize"]), stage.Parameters["sigmaX"]);
                    SaveImage(image.ToBitmap(), imagePath, @$"GaussianBlur-{stage.Parameters["ksize"]}-{stage.Parameters["ksize"]}-{stage.Parameters["sigmaX"]}", callStack);
                }
                else if (stage.FunctionName == "MedianBlur")
                {
                    Cv2.MedianBlur(image, image, (int)stage.Parameters["ksize"]);
                    SaveImage(image.ToBitmap(), imagePath, @$"MedianBlur-{(int)stage.Parameters["ksize"]}", callStack);
                }
                else if (stage.FunctionName == "FastNlMeansDenoising")
                {
                    Cv2.FastNlMeansDenoising(image, image, (float)stage.Parameters["h"], (int)stage.Parameters["templateWindowSize"], (int)stage.Parameters["searchWindowSize"]);
                    SaveImage(image.ToBitmap(), imagePath, @$"FastNlMeansDenoising-{(float)stage.Parameters["h"]}-{(int)stage.Parameters["templateWindowSize"]}-{(int)stage.Parameters["searchWindowSize"]}", callStack);
                }
            }
            return image;
        }

        private Mat ApplyContrastBrightnessAdjustment(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute && stage.FunctionName == "ConvertScaleAbs")
            {
                Cv2.ConvertScaleAbs(image, image, alpha: stage.Parameters["alpha"], beta: stage.Parameters["beta"]);
                SaveImage(image.ToBitmap(), imagePath, @$"ConvertScaleAbs-{stage.Parameters["alpha"]}-{stage.Parameters["beta"]}", callStack);
            }
            return image;
        }

        private Mat ApplySharpening(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute)
            {
                if (stage.FunctionName == "Filter2D")
                {
                    float kernelCentralValue = (float)stage.Parameters["kernelCentralValue"];
                    float surroundValue = -1 * (kernelCentralValue - 1) / 8;
                    using var kernel = new Mat();
                    float[,] data = new float[,]
                    {
                    { surroundValue,    surroundValue,      surroundValue },
                    { surroundValue,    kernelCentralValue, surroundValue },
                    { surroundValue,    surroundValue,      surroundValue }
                    };
                    kernel.Create(new OpenCvSharp.Size(3, 3), MatType.CV_32F);
                    Marshal.Copy(data.Cast<float>().ToArray(), 0, kernel.Data, 9);
                    Cv2.Filter2D(image, image, (int)stage.Parameters["ddepth"], kernel, new OpenCvSharp.Point(-1, -1));
                    SaveImage(image.ToBitmap(), imagePath, @$"Filter2D-{stage.Parameters["kernelCentralValue"]}", callStack);
                }
                else if (stage.FunctionName == "Laplacian")
                {
                    Cv2.Laplacian(image, image, MatType.CV_64F);
                    SaveImage(image.ToBitmap(), imagePath, @$"Laplacian-{MatType.CV_64F}", callStack);
                }
            }
            return image;
        }

        private Mat ApplyBinarization(Mat image, ImageProcessingStage stage, string imagePath, string callStack = "")
        {
            if (stage.Execute && stage.FunctionName == "AdaptiveThreshold")
            {
                Cv2.AdaptiveThreshold(image, image, stage.Parameters["maxValue"], (AdaptiveThresholdTypes)stage.Parameters["adaptiveMethod"], (ThresholdTypes)stage.Parameters["thresholdType"], (int)stage.Parameters["blockSize"], stage.Parameters["C"]);
                SaveImage(image.ToBitmap(), imagePath, @$"AdaptiveThreshold-{stage.Parameters["maxValue"]}-{stage.Parameters["adaptiveMethod"]}-{stage.Parameters["thresholdType"]}-{stage.Parameters["blockSize"]}-{stage.Parameters["C"]}", callStack);
            }
            else if (stage.Execute && stage.FunctionName == "Threshold")
            {
                Cv2.Threshold(image, image, stage.Parameters["thresh"], stage.Parameters["maxval"], (ThresholdTypes)stage.Parameters["type"]);
                SaveImage(image.ToBitmap(), imagePath, @$"Threshold-{stage.Parameters["thresh"]}-{stage.Parameters["maxval"]}-{stage.Parameters["type"]}", callStack);
            }
            return image;
        }




        public void SaveImage(Bitmap image, string imagePath, string nameModif, string callStack = "")
        {// запись

            string processedFilePath = Path.Combine(
                                        Path.GetDirectoryName(imagePath),
                                        "processed",
                                        $"{Path.GetFileNameWithoutExtension(imagePath)}-{callStack}-{nameModif}{Path.GetExtension(imagePath)}");

            Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath));
            image.Save(processedFilePath, ImageFormat.Png);
        }








        private Bitmap ApplyImageProcessing2(Bitmap sourceImage, string imagePath, ImageProcessingParameters parameters)
        {
            //using Mat matImage = BitmapConverter.ToMat(sourceImage);
            Mat resultImage = null;  //matImage;

            var decoder1 = new QRRecognitionOpenCV();
            var decoder2 = new QRRecognitionZXing();

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < 10; i++)
            {
                resultImage = sourceImage.ToMat();

                Cv2.Resize(resultImage, resultImage, new OpenCvSharp.Size(0, 0), fx: 0.8, fy: 0.8, interpolation: InterpolationFlags.Cubic);
                SaveImage(resultImage.ToBitmap(), imagePath, $@"resize-{i}");


                // яркость и контраст
                parameters.convScaleContrast = 1.2;
                parameters.convScaleBrightness = 10.0;
                Cv2.ConvertScaleAbs(resultImage, resultImage, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость

                var qrcode = decoder1.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode == null)
                    qrcode = decoder2.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode != null)
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"bright-OK-{i}");
                else
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"bright-{i}");

                // Удаление шума
                parameters.medianBlurKernel = 3;
                Cv2.FastNlMeansDenoising(resultImage, resultImage, (int)parameters.medianBlurKernel); //+ 

                qrcode = decoder1.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode == null)
                    qrcode = decoder2.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode != null)
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"denoise-OK-{i}");
                else
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"denoise-{i}");


                // Повышение резкости 
                float centerValue_ = 9.0f;    // (float) parameters.sharpLevel;
                float surroundValue_ = -1;   // -centerValue / 8.0f;

                using var kernel_ = new Mat();
                float[,] data_ = new float[,]
                {
                                    { surroundValue_, surroundValue_, surroundValue_ },
                                    { surroundValue_,  centerValue_,  surroundValue_ },
                                    { surroundValue_, surroundValue_, surroundValue_ }
                };
                kernel_.Create(new OpenCvSharp.Size(3, 3), MatType.CV_32F);
                Marshal.Copy(data_.Cast<float>().ToArray(), 0, kernel_.Data, 9);
                Cv2.Filter2D(resultImage, resultImage, -1, kernel_, new OpenCvSharp.Point(-1, -1));

                qrcode = decoder1.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode == null)
                    qrcode = decoder2.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode != null)
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"sharp-OK-{i}");
                else
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"sharp-{i}");


                // Бинаризация ThresholdTypes.Tozero |
                parameters.adThreshBlock = 49;
                parameters.adThreshC = 12;
                //Cv2.Threshold(resultImage, resultImage, parameters.adThreshBlock, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                Cv2.AdaptiveThreshold(resultImage, resultImage, 255,
                                        AdaptiveThresholdTypes.MeanC,// GaussianC,
                                        ThresholdTypes.Binary, // | ThresholdTypes.Otsu, //ThresholdTypes.Otsu,
                                        (int)parameters.adThreshBlock,
                                        (int)parameters.adThreshC);

                qrcode = decoder1.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode == null)
                    qrcode = decoder2.DecodeQRCode(resultImage.ToBitmap());
                if (qrcode != null)
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"threshold-OK-{i}");
                else
                    SaveImage(resultImage.ToBitmap(), imagePath, $@"threshold-{i}");


            }

            return resultImage.ToBitmap();




            //  Усиление яркости и контраста
            if (parameters.convScaleContrast != 1.0 || parameters.convScaleBrightness != 0.0)
            {
                Mat enhanced = new Mat();
                Cv2.ConvertScaleAbs(resultImage, enhanced, alpha: parameters.convScaleContrast, beta: parameters.convScaleBrightness); //+ Контраст и Яркость
                resultImage = enhanced;

                SaveImage(resultImage.ToBitmap(), imagePath, $@"-bright");
            }

            // Удаление шума
            if (parameters.medianBlurKernel > 0)
            {
                Mat denoised = new Mat();
                Cv2.FastNlMeansDenoising(resultImage, denoised, (int)parameters.medianBlurKernel); //+ 
                resultImage = denoised;

                SaveImage(resultImage.ToBitmap(), imagePath, $@"-noise");
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
                kernel.Create(new OpenCvSharp.Size(3, 3), MatType.CV_32F);
                Marshal.Copy(data.Cast<float>().ToArray(), 0, kernel.Data, 9);


                Mat sharp = new Mat();
                Cv2.Filter2D(resultImage, sharp, -1, kernel, new OpenCvSharp.Point(-1, -1));
                resultImage = sharp;

                SaveImage(resultImage.ToBitmap(), imagePath, $@"-sharp");
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

                SaveImage(resultImage.ToBitmap(), imagePath, $@"-thresh");
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

                SaveImage(resultImage.ToBitmap(), imagePath, $@"-morf");
            }

            return resultImage.ToBitmap();
        }


    }
}

/*

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
                    using var bitmap = BitmapConverter.ToBitmap(resultImage);
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
*/

