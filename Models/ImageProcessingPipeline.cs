
using Google.Protobuf.WellKnownTypes;

namespace UpRestEye3.Models
{
    public class ImageDigest
    {
        public float brightness { get; set; }      // Уровень яркости
        public float contrast { get; set; }        // Уровень контраста
        public float noiseLevel { get; set; }      // Уровень шума
        public float sharpness { get; set; }       // Уровень резкости
        public float aspectRatio { get; set; }     // Соотношение сторон
        public ImageType imageType { get; set; }   // Тип изображения
        public ImageDigest()
        {
            brightness = 0;
            contrast = 0;
            noiseLevel = 0;
            sharpness = 0;
            aspectRatio = 0;
            imageType = ImageType.Clean;
        }

        [Flags]
        public enum ImageType
        {
            Dark = 1,
            Light = 2,
            Contrast = 4,
            UnContrast = 8,
            Blurred = 16,
            Focused = 32,
            Noisy = 64,
            Clean = 128
        }

    }


    public class ImageProcessingParameters
    {
        public double medianBlurKernel { get; set; } = 0.0;         // Для медианного фильтра - удаление шумов
        public double convScaleContrast { get; set; } = 0.0;        // Уровень контраста
        public double convScaleBrightness { get; set; } = 0.0;      // Уровень яркости
        public double sharpLevel { get; set; } = 0.0;               // Уровень резкости (новое)
        public double adThreshBlock { get; set; } = 0.0;            // Для адаптивной бинаризации
        public double adThreshC { get; set; } = 0.0;                // Для адаптивной бинаризации
        public double cannyThreshold1 { get; set; } = 0.0;          // Для обнаружения линий, нижняя граница, для QR 80
        public double cannyThreshold2 { get; set; } = 0.0;          // Для обнаружения линий, верхняя граница, для QR 160
        public double sharpWeightA { get; set; } = 0.0;              // Вес резкости альфа
        public double sharpWeightB { get; set; } = 0.0;              // Вес резкости бетта



       





        public bool Any()
        {
            return medianBlurKernel != 0 ||
                    convScaleContrast != 0.0 ||
                    convScaleBrightness != 0.0 ||
                    sharpLevel != 0.0 ||
                    adThreshBlock != 0.0 ||
                    adThreshC != 0.0 ||
                    cannyThreshold1 != 0.0 ||
                    cannyThreshold2 != 0.0 ||
                    sharpWeightA !=0.0 ||
                    sharpWeightB !=0.0;
        }

        public void Clear()
        {
            medianBlurKernel = 0.0;
            convScaleContrast = 0.0;
            convScaleBrightness = 0.0;
            sharpLevel = 0.0;
            adThreshBlock = 0.0;
            adThreshC = 0.0;
            cannyThreshold1 = 0.0;
            cannyThreshold2 = 0.0;
            sharpWeightA = 0.0;
            sharpWeightB = 0.0;
        }
        public void SetMedium()
        {
            medianBlurKernel = 1;
            convScaleContrast = 1.2;
            convScaleBrightness = 10.0;
            sharpLevel = 1.0;
            adThreshBlock = 0;
            adThreshC = 0;
            cannyThreshold1 = 0;
            cannyThreshold2 = 0;
            sharpWeightA = 0;
            sharpWeightB = 0;
        }
    }

    public class ImageProcessingHypotheses 
    {

        // Список параметров для итераций
        // Для медианного фильтра - удаление шумов
        public double[] medianBlurKernels { get; } = { 0.0, 3, 5, 7 };
        
        // Уровень контраста
        public double[] convScaleContrasts { get; } = { 0.0, 1.7, 1.2, 1.5, 2.0 };

        // Уровень яркости
        public double[] convScaleBrightnesses { get; } = { 0.0, 20.0, -10.0, 10.0, -20.0 }; 
        
        // Для  бинаризации
        public double[] adThreshBlocks { get; } = { 0.0, 100, 127, 150 };

        // Для адаптивной бинаризации
        //double[] adThreshBlocks { get; } = { 0, 3, 7, 11 };                        

        // Для обнаружения линий
        public double[,] cannyThresholds { get; } = new double[,] { { 0.0, 0.0 }, { 80, 160 }, { 50, 150 }, { 10, 100 }, { 100, 200 } }; 

        // Вес резкости
        public double[,] sharpWeights { get; } = new double[,] { { 0.0, 0.0 }, { 1.5, -0.5 }, { 0.7, 0.3 } }; 
        
    }

    public class ImageProcessingStage
    {
        public bool Execute { get; set; }
        public string FunctionName { get; set; }
        public Dictionary<string, double> Parameters { get; set; }

        public ImageProcessingStage()
        {
            Execute = false;
        }
    }

    // выстраиваем конвейер обработки изображения
    // 0. Оттенки серого: CvtColor
    // 1. Изменение размера: Resize
    // 2. Увеличение контраста и яркости: ConvertScaleAbs
    // 3. Удаление шумов: GaussianBlur / MedianBlur / FastNlMeansDenoising
    // 4. Повышение резкости: Filter2D / Laplacian
    // 5. Бинаризация: Threshold / AdaptiveThreshold
    // 6. Удаление шумов: GaussianBlur / MedianBlur / FastNlMeansDenoising
    public class ImageProcessingPipeline
    {
        public ImageProcessingStage Grayscale { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage Resize { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage ContrastBrightnessAdjustment { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage NoiseRemoval { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage Sharpening { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage Binarization { get; set; } = new ImageProcessingStage();
        public ImageProcessingStage FinalNoiseRemoval { get; set; } = new ImageProcessingStage();

        public ImageProcessingPipeline()
        {
        }

        public bool Any()
        {
            return Grayscale.Execute || 
                    Resize.Execute || 
                    ContrastBrightnessAdjustment.Execute || 
                    NoiseRemoval.Execute || 
                    Sharpening.Execute || 
                    Binarization.Execute || 
                    FinalNoiseRemoval.Execute;
        }
    }
    public class CvtColorStage : ImageProcessingStage
    {
        public CvtColorStage(double code)
        {
            Execute = true;
            FunctionName = "CvtColor";
            Parameters = new Dictionary<string, double> { { "code", code } };
        }
    }

    public class ResizeStage : ImageProcessingStage
    {
        public ResizeStage(double fx, double fy)
        {
            Execute = true;
            FunctionName = "Resize";
            Parameters = new Dictionary<string, double> { { "fx", fx }, { "fy", fy } };
        }
    }

    public class GaussianBlurStage : ImageProcessingStage
    {
        public GaussianBlurStage(double ksize, double sigmaX)
        {
            Execute = true;
            FunctionName = "GaussianBlur";
            Parameters = new Dictionary<string, double> { { "ksize", ksize }, { "sigmaX", sigmaX } };
        }
    }

    public class MedianBlurStage : ImageProcessingStage
    {
        public MedianBlurStage(double ksize)
        {
            Execute = true;
            FunctionName = "MedianBlur";
            Parameters = new Dictionary<string, double> { { "ksize", ksize } };
        }
    }

    public class FastNlMeansDenoisingStage : ImageProcessingStage
    {
        public FastNlMeansDenoisingStage(double h, double templateWindowSize, double searchWindowSize)
        {
            Execute = true;
            FunctionName = "FastNlMeansDenoising";
            Parameters = new Dictionary<string, double> { { "h", h }, { "templateWindowSize", templateWindowSize }, { "searchWindowSize", searchWindowSize } };
        }
    }

    public class ConvertScaleAbsStage : ImageProcessingStage
    {
        public ConvertScaleAbsStage(double alpha, double beta)
        {
            Execute = true;
            FunctionName = "ConvertScaleAbs";
            Parameters = new Dictionary<string, double> { { "alpha", alpha }, { "beta", beta } };
        }
    }

    public class Filter2DStage : ImageProcessingStage
    {
        public Filter2DStage(double ddepth, double kernel)
        {
            Execute = true;
            FunctionName = "Filter2D";
            Parameters = new Dictionary<string, double> { { "ddepth", ddepth }, { "kernel", kernel } };
        }
    }

    public class LaplacianStage : ImageProcessingStage
    {
        public LaplacianStage(double ddepth, double kernelCentralValue)
        {
            Execute = true;
            FunctionName = "Laplacian";
            Parameters = new Dictionary<string, double> { { "ddepth", ddepth }, { "kernelCentralValue", kernelCentralValue } };
        }
    }

    public class ThresholdStage : ImageProcessingStage
    {
        public ThresholdStage(double thresh, double maxval, double type)
        {
            Execute = true;
            FunctionName = "Threshold";
            Parameters = new Dictionary<string, double> { { "thresh", thresh }, { "maxval", maxval }, { "type", type } };
        }
    }

    public class AdaptiveThresholdStage : ImageProcessingStage
    {
        public AdaptiveThresholdStage(double maxValue, double adaptiveMethod, double thresholdType, double blockSize, double C)
        {
            Execute = true;
            FunctionName = "AdaptiveThreshold";
            Parameters = new Dictionary<string, double> { { "maxValue", maxValue }, { "adaptiveMethod", adaptiveMethod }, { "thresholdType", thresholdType }, { "blockSize", blockSize }, { "C", C } };
        }
    }
}