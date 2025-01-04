using OpenCvSharp;
using System.Collections.Generic;
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
    public class ImageProcessingPipelineHelper : IImageProcessingPipelineHelper
    {
        private readonly List<(ImageDigest.ImageType, ImageProcessingPipeline)> _pipelines;

        public ImageProcessingPipelineHelper()
        {
            _pipelines = new List<(ImageDigest.ImageType, ImageProcessingPipeline)>();
            BuildPipelines();
        }


        // TODO сделать множество пайплайнов
        private void BuildPipelines()
        {
            // Темное + в фокусе
            var darkFocusedPipeline = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.5, 50),
                NoiseRemoval = new MedianBlurStage(5),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Focused, darkFocusedPipeline));

            // Темное + в фокусе + чистое
            var darkFocusedCleanPipeline = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.2, 30),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2)
            };
            _pipelines.Add((ImageDigest.ImageType.Dark | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, darkFocusedCleanPipeline));

            // Светлое + в фокусе
            var lightFocusedPipeline = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(0.8, -30),
                NoiseRemoval = new MedianBlurStage(5),
                Sharpening = new Filter2DStage(-1, 1.0),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2),
                FinalNoiseRemoval = new GaussianBlurStage(5, 1.5)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Focused, lightFocusedPipeline));

            // Светлое + в фокусе + чистое
            var lightFocusedCleanPipeline = new ImageProcessingPipeline
            {
                Grayscale = new CvtColorStage(6),
                Resize = new ResizeStage(1.2, 1.2),
                ContrastBrightnessAdjustment = new ConvertScaleAbsStage(1.0, -10),
                Sharpening = new Filter2DStage(-1, 0.7),
                Binarization = new AdaptiveThresholdStage(255, 0, 0, 11, 2)
            };
            _pipelines.Add((ImageDigest.ImageType.Light | ImageDigest.ImageType.Focused | ImageDigest.ImageType.Clean, lightFocusedCleanPipeline));

            // Add other pipelines similarly...

        }

        public List<(ImageDigest.ImageType, ImageProcessingPipeline)> GetPipelines()
        {
            return _pipelines;
        }

        public ImageProcessingPipeline GetCalculatedPipeline(ImageDigest digest)
        {
            // Implement logic to select the best pipeline based on the digest
            return _pipelines.Where(a => a.Item1 == digest.imageType).FirstOrDefault().Item2;
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
    }
}

