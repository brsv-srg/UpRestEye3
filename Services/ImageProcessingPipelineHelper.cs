using System.Collections.Generic;
using System.Drawing;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{
    public interface IImageProcessingPipelineHelper
    {
        List<ImageProcessingPipeline> BuildPipelines();
        ImageProcessingPipeline GetCalculatedPipeline(ImageDigest digest);
    }

    // Класс обработки изображения
    public class ImageProcessingPipelineHelper: IImageProcessingPipelineHelper
    {
        ImageProcessingPipelineHelper()
        {
        }

        public List<ImageProcessingPipeline> BuildPipelines()
        {
            var pipelines = new List<ImageProcessingPipeline>();

            // Темное + в фокусе
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new MedianBlurStage(5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 50),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Темное + в фокусе + чистое
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 30),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2)
            });

            // Светлое + в фокусе
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new MedianBlurStage(5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Светлое + в фокусе + чистое
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.0, -10),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2)
            });

            // Темное + в расфокусе
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new MedianBlurStage(7),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 50),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            // Светлое + в расфокусе
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new MedianBlurStage(7),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            // Темное + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 50),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Светлое + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Темное + в расфокусе + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 50),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            // Светлое + в расфокусе + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            // Темное + в фокусе + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 50),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Светлое + в фокусе + зашумленное
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            });

            // Темное + в расфокусе + чистое
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 50),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            // Светлое + в расфокусе + чистое
            pipelines.Add(new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            });

            return pipelines;
        }

        // TODO сделать подбор параметров на основе метрик
        public ImageProcessingPipeline GetCalculatedPipeline(ImageDigest digest)
        {
            return new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            };

        }

        // TODO сделать умолчательный пайплайн
        public ImageProcessingPipeline GetDefaultPipeline()
        {
            return new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(800, 600),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(7, 1.5)
            };

        }

    }
}



/*

public ImageProcessingParameters GetProbable(ImageDigest digest)
        {
            try
            {
                var prediction = new ImageProcessingParameters();


                // Адаптивная коррекция яркости и контраста
                //if (metrics.Brightness < 100)
                //{
                //    convScaleContrast 1.2, convScaleBrightness 10
                //}
                //else if (metrics.Brightness > 200)
                //{
                //    convScaleContrast 0.8, convScaleBrightness - 10
                //}


                // Вычисляем параметры на основе метрик
                prediction.convScaleContrast = 1.0;         // начальное значение для контраста
                prediction.convScaleBrightness = 0.0;       // начальное значение для яркости

                // Настройка для темных изображений
                if (digest.brightness < 100)
                {
                    // Плавная настройка alpha от 1.0 до 1.4
                    prediction.convScaleContrast = 1.0 + ((100 - digest.brightness) / 100.0) * 0.4;
                    // Плавная настройка beta от 0 до 30
                    prediction.convScaleBrightness = ((100 - digest.brightness) / 100.0) * 30;
                }
                // Настройка для светлых изображений
                else if (digest.brightness > 200)
                {
                    // Плавная настройка alpha от 1.0 до 0.6
                    prediction.convScaleContrast = 1.0 - ((digest.brightness - 200) / 55.0) * 0.4;
                    // Плавная настройка beta от 0 до -30
                    prediction.convScaleBrightness = -((digest.brightness - 200) / 55.0) * 30;
                }

                // Учитываем контраст
                if (digest.contrast < 30)
                {
                    // Увеличиваем alpha для повышения контраста
                    prediction.convScaleContrast *= 1.2;
                }



                // Адаптивное шумоподавление
                 if (digest.noiseLevel > 10) 
                    prediction.medianBlurKernel = (int)(digest.noiseLevel / 2);

                // Адаптивное усиление резкости в зависимости от уровня размытия
                if (digest.sharpness < 10)
                    prediction.sharpLevel = 1.0;
                else
                    prediction.sharpLevel = 1 + (digest.sharpness / 10.0);


                // Адаптивная бинаризация
                // adThreshBlock = metrics.Contrast < 30 ? 15 : 11;
                // double C = metrics.Contrast < 30 ? 4 : 2;

                // Адаптивная настройка размера блока
                prediction.adThreshBlock = Math.Max(3, Math.Min(19, (int)(11 + (30 - digest.contrast) / 2)));
                prediction.adThreshBlock += (prediction.adThreshBlock % 2 == 0) ? 1 : 0; // Убеждаемся что нечетное

                // Адаптивная настройка константы C
                prediction.adThreshC = Math.Max(1, Math.Min(5, 2 + (30 - digest.contrast) / 10));


                return prediction;
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred during prediction: {ex.Message}");
                // Return default or null to indicate failure
                return null;
            }
        }
 */
