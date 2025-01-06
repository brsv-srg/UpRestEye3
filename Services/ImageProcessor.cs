using UpRestEye3.Models;
using System.Drawing;



namespace UpRestEye3.Services
{
    
  

    public interface IImageFileProcessor
    {
        Task<(QRCodeData?, Bitmap?)> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<(QRCodeData?, Bitmap?)> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<Invoice> DeepTextRecognitionAsync(Bitmap sourceImage, Invoice currentInvoice);

    }

    // Класс обработки изображения
    public class ImageProcessor: IImageFileProcessor
    {
        private readonly ILocalMLService _predictor;
        private readonly IGPTService _gptParser;
        private readonly IQRProcessing _qrProcessor;
        private readonly IEnumerable<IQRRecognition> _qrRecognizers;
        private readonly ITextRecognition _textRecognizer;
        private readonly IImageProcessingPipelineHelper _pipelineHelper;

        public ImageProcessor(IQRProcessing qrProcessor, ILocalMLService predictor, IGPTService gptParser, IEnumerable<IQRRecognition> qrRecognizers, ITextRecognition textRecognizer, IImageProcessingPipelineHelper pipelineHelper)
        {
            _predictor = predictor;
            _qrProcessor = qrProcessor;
            _qrRecognizers = qrRecognizers;
            _textRecognizer = textRecognizer;
            _gptParser = gptParser;
            _pipelineHelper = pipelineHelper;
        }

        public async Task<(QRCodeData?, Bitmap?)> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
            Bitmap? resultImage = null;
            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(sourceImage);

            // Шаг 2. Попытка распознать "в лоб"
            if (TryDecodeQRCode(sourceImage, out QRCodeData? qrCodeData))
                return (qrCodeData, sourceImage);

            // Шаг 3. Обработка по пайплайну от модели
            var predictedPipeline = _predictor.Predict(digest);
            if (predictedPipeline != null && predictedPipeline.Any())
            {
                var processedImagePred = _qrProcessor.ApplyImageProcessing(sourceImage, predictedPipeline, imagePath, "basicQR-predPL");
                if (TryDecodeQRCode(processedImagePred, out qrCodeData))
                    return (qrCodeData, processedImagePred);
            }

            // Шаг 4. Обработка по алгоритмически подбранному по дайджесту пайплайну
            var calculatedPipeline = _pipelineHelper.GetCalculatedPipeline(digest);
            if (calculatedPipeline != null)
            {
                var processedImageCalc = _qrProcessor.ApplyImageProcessing(sourceImage, calculatedPipeline, imagePath, "basicQR-calcPL");
                if (TryDecodeQRCode(processedImageCalc, out qrCodeData))
                {
                    _predictor.UpdateModel(digest, calculatedPipeline); // Обучение
                    return (qrCodeData, processedImageCalc);
                }
            }

            // Шаг 5. На всякий случай пробумем по дефолтному пайплайну на основе умолчательного конструктора
            var defaultPipeline = _pipelineHelper.GetDefaultPipeline();
            var processedImageDef = _qrProcessor.ApplyImageProcessing(sourceImage, defaultPipeline, imagePath, "basicQR-defPL");
            if (TryDecodeQRCode(processedImageDef, out qrCodeData))
            { 
                _predictor.UpdateModel(digest, defaultPipeline); // Обучение
                return (qrCodeData, processedImageDef);
            }
            return (null,null);
        }

        public async Task<(QRCodeData?, Bitmap?)> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(sourceImage);

            // Шаг 2. Создание набора пайплайнов на все случаи жизни 
            var pipelines = _pipelineHelper.GetPipelines();
            
            // Шаг 3. Обработка всех вариантов в цикле
            foreach (var pipeline in pipelines)
            {
                var processedImage = _qrProcessor.ApplyImageProcessing(sourceImage, pipeline.Item2, imagePath, @$"deepQR-allPLs-{pipeline.Item1}");
                if (TryDecodeQRCode(processedImage, out QRCodeData? qrCodeData))
                {
                    _predictor.UpdateModel(digest, pipeline.Item2); // Обучение модели
                    return (qrCodeData, processedImage);
                }
            }
            return (null, null);
        }

        public async Task<Invoice?> DeepTextRecognitionAsync(Bitmap sourceImage, Invoice currentInvoice)
        {
            // Обращение к внешней модели
            var recognizedText = await _textRecognizer.TextRecognize(sourceImage);
            if (recognizedText != null)
            {
                return await _gptParser.ParseReceiptWithLLM(recognizedText, currentInvoice);
            }
            return null;
        }

        private bool TryDecodeQRCode(Bitmap sourceImage, out QRCodeData? qrCodeData)
        {
            qrCodeData = null;
            foreach (var _qrRecognizer in _qrRecognizers)
            {
                var qrCodeDataLocal = _qrRecognizer.DecodeQRCode(sourceImage);
                if (qrCodeDataLocal != null)
                {
                    qrCodeData = qrCodeDataLocal;
                    return true;
                }
            }
            return false;
        }

    }
}