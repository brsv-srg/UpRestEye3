using AForge.Imaging.Filters;
using OpenCvSharp;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using Tensorflow;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{
    public interface IImageProcessingPipelineHelper
    {
        List<(ImageDigest.ImageType, ImageProcessingPipeline)> GetPipelines();
        ImageProcessingPipeline GetCalculatedPipeline(ImageDigest digest);
        ImageProcessingPipeline GetDefaultPipeline();
    }

    // Класс обработки изображения
    public class ImagePipelineHelper : IImageProcessingPipelineHelper
    {
        private readonly List<(ImageDigest.ImageType, ImageProcessingPipeline)> _pipelines;

        public ImagePipelineHelper()
        {
            _pipelines = new List<(ImageDigest.ImageType, ImageProcessingPipeline)>();
            BuildPipelines();
        }



        public List<(ImageDigest.ImageType, ImageProcessingPipeline)> GetPipelines()
        {
            return _pipelines;
        }


        public ImageProcessingPipeline GetCalculatedPipeline(ImageDigest digest)
        {
            var imageType = digest.imageType;
            var result = _pipelines.FirstOrDefault(a => a.Item1 == imageType).Item2;

            if (result == null)
            {
                imageType = imageType & ~ImageDigest.ImageType.Contrast;
                imageType = imageType & ~ImageDigest.ImageType.UnContrast; 
                result = _pipelines.FirstOrDefault(a => a.Item1 == imageType).Item2;

                if (result == null)
                {
                    imageType = imageType & ~ImageDigest.ImageType.Blurred;
                    imageType = imageType & ~ImageDigest.ImageType.Focused;
                    result = _pipelines.FirstOrDefault(a => a.Item1 == imageType).Item2;

                    if (result == null)
                    {
                        imageType = imageType & ~ImageDigest.ImageType.Clean;
                        imageType = imageType & ~ImageDigest.ImageType.Noisy;
                        result = _pipelines.FirstOrDefault(a => a.Item1 == imageType).Item2;
                    }
                }
            }

            return result;

        }

        public ImageProcessingPipeline GetDefaultPipeline()
        {
            // Умолчательный безобидный пайплайн
            return new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 0),
                NoiseRemoval = new FastNlMeansDenoisingStage(3, 7, 21),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
            };
        }

        private void BuildPipelines()
        {
            // Темное + в фокусе
            var darkFocused = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 30),
                NoiseRemoval = new MedianBlurStage(5),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 45, 20),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Focused, darkFocused));

            // Темное + контрастное + в фокусе + чистое
            var darkContrastFocusedClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 20),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 50, 25)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, darkContrastFocusedClean));
            
            // Темное + неконтрастное + в фокусе + чистое
            var darkUnContrastFocusedClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, 30),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 50, 25)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, darkUnContrastFocusedClean));

            // Светлое + в фокусе
            var lightFocused = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -20),
                NoiseRemoval = new MedianBlurStage(5),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Focused, lightFocused));

            // Светлое + контрастное + в фокусе + чистое
            var lightContrastFocusedClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.0, -10),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, lightContrastFocusedClean));

            // Светлое + неконтрастное + в фокусе + чистое
            var lightUnContrastFocusedClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, -10),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, lightUnContrastFocusedClean));

            // Темное + в расфокусе
            var darkBlurred = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 20),
                NoiseRemoval = new MedianBlurStage(3),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 40, 17),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Blurred, darkBlurred));

            // Темное + контрастное +  в расфокусе + чистое
            var darkContrastBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 30),
                NoiseRemoval = new MedianBlurStage(3),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 40, 17),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, darkContrastBlurredClean));
            
            // Темное + в расфокусе
            var darkUnContrastBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, 20),
                NoiseRemoval = new MedianBlurStage(3),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 40, 17),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, darkUnContrastBlurredClean));

            // Светлое + в расфокусе
            var lightBlurred = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -25),
                NoiseRemoval = new MedianBlurStage(3),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu))
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Blurred, lightBlurred));

            // Темное + зашумленное
            var darkNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.3, 1.3),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 20),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Noisy, darkNoisy));

            // Светлое + зашумленное
            var lightNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.3, 1.3),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
                FinalNoiseRemoval = new GaussianBlurStage(3, 1)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Noisy, lightNoisy));

            // Темное + в расфокусе + зашумленное
            var darkBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 40),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new ThresholdStage(0, 255, (double)ThresholdTypes.Otsu),
                FinalNoiseRemoval = new GaussianBlurStage(3, 1)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, darkBlurredNoisy));

            // Темное + не контрастное + в расфокусе + зашумленное
            var darkUnContrastBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, 30),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 55, 20),
                FinalNoiseRemoval = new GaussianBlurStage(3, 1)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, darkUnContrastBlurredNoisy));
           
            // Темное + контрастное + в расфокусе + зашумленное
            var darkContrastBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 10),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 55, 20),
                FinalNoiseRemoval = new GaussianBlurStage(3, 1)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, darkContrastBlurredNoisy));

            // Светлое + в расфокусе + зашумленное
            var lightBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, lightBlurredNoisy));

            // Светлое + контрастное + в расфокусе + зашумленное
            var lightContrastBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -20),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, lightContrastBlurredNoisy));

            // Светлое + неконтрастное + в расфокусе + зашумленное
            var lightUnContrastBlurredNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, -10),
                NoiseRemoval = new FastNlMeansDenoisingStage(7, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Noisy, lightUnContrastBlurredNoisy));


            // Темное + в фокусе + зашумленное
            var darkFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 25),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 50, 20),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, darkFocusedNoisy));

            // Темное + контрастное + в фокусе + зашумленное
            var darkContrastFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 35),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 20, 7),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, darkContrastFocusedNoisy));

            // Темное + не контрастное + в фокусе + зашумленное
            var darkUnContrastFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, 30),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 20, 7),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, darkUnContrastFocusedNoisy));

            // Светлое + в фокусе + зашумленное
            var lightFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, lightFocusedNoisy));

            // Светлое + контрастное + в фокусе + зашумленное
            var lightContrastFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -20),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, lightContrastFocusedNoisy));
            
            // Светлое + не контрастное + в фокусе + зашумленное
            var lightUnContrastFocusedNoisy = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, -30),
                NoiseRemoval = new FastNlMeansDenoisingStage(5, 7, 21),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Noisy, lightUnContrastFocusedNoisy));


            // Темное + в расфокусе + чистое
            var darkBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.3, 1.3),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 20),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 55, 25),
                FinalNoiseRemoval = new GaussianBlurStage(3, 0.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, darkBlurredClean));


            // Светлое + в расфокусе + чистое
            var lightBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -10),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new ThresholdStage(0, 255, (double)(ThresholdTypes.Binary | ThresholdTypes.Otsu)),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, lightBlurredClean));

            // Светлое + контрастное + в расфокусе + чистое
            var lightContrastBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -10),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 20, 5),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Contrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, lightContrastBlurredClean));
            
            // Светлое + не контрастное + в расфокусе + чистое
            var lightUnContrastBlurredClean = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.5, 1.5),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.7, 0),
                Sharpening = new Filter2DStage(-1, 1.5),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 20, 5),
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.UnContrast | ImageDigest.ImageType.Blurred | ImageDigest.ImageType.Clean, lightUnContrastBlurredClean));
        }


    }
}

