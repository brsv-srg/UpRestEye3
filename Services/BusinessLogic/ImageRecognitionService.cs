using System.Drawing;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;



namespace UpRestEye3.Services.BusinessLogic
{



    public interface IImageRecognitionService
    {
        Task<(QRCodeData, Bitmap)> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<(QRCodeData, Bitmap)> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath);
        Task<InvoiceDTO?> DeepTextRecognitionAsync(List<Bitmap> sourceImages, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits);
      

    }

    // Класс обработки изображения
    public class ImageRecognitionService : IImageRecognitionService
    {
        private readonly ILocalMLService _predictor;
        private readonly IGPTSemanticService _gptParser;
        private readonly IGPTLayoutService _gptLayout;
        private readonly IQRProcessing _qrProcessor;
        private readonly IEnumerable<IQRRecognition> _qrRecognizers;
        private readonly ITextRecognition _textRecognizer;
        private readonly IImagePipelineHelper _pipelineHelper;

        public ImageRecognitionService(IQRProcessing qrProcessor, ILocalMLService predictor, IGPTSemanticService gptParser, IGPTLayoutService gptLayout, IEnumerable<IQRRecognition> qrRecognizers, ITextRecognition textRecognizer, IImagePipelineHelper pipelineHelper)
        {
            _predictor = predictor;
            _qrProcessor = qrProcessor;
            _qrRecognizers = qrRecognizers;
            _textRecognizer = textRecognizer;
            _gptParser = gptParser;
            _gptLayout = gptLayout;
            _pipelineHelper = pipelineHelper;
        }

        public async Task<(QRCodeData, Bitmap)> BasicQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {
           
            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(sourceImage);

            // Шаг 2. Попытка распознать "в лоб"
            if (TryDecodeQRCode(sourceImage, out QRCodeData? qrCodeData))
                return ((qrCodeData, sourceImage));

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
           
            // Если не удалось распознать QR-код, добавляем в результат null
            return (null, sourceImage);
        
        }


        public async Task<(QRCodeData, Bitmap)> DeepQRRecognitionAsync(Bitmap sourceImage, string imagePath)
        {


            // Шаг 1. Создание дайджеста изображения
            var digest = _qrProcessor.GenerateImageDigest(sourceImage);

            // Шаг 2. Создание набора пайплайнов на все случаи жизни 
            var pipelines = _pipelineHelper.GetPipelines();
            bool isQRGotten = false;
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

            return (null, sourceImage);
        }


        public async Task<InvoiceDTO?> DeepTextRecognitionAsync(List<Bitmap> sourceImages, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var textProcessor = new RecognizedTextProcessor();

            // тут собираем все таблицы с продуктами с нескльких страниц в один документ
            var allPagesData = new TablesDataDocument()
            {
                Pages = new List<TablesDataPage>()
            };

            foreach (var sourceImage in sourceImages)
            {
                // Обращение к внешней OCR
                var recognizedText = await _textRecognizer.TextRecognize(sourceImage);
                
                if (recognizedText == null)
                    throw new Exception($"Text recognition error: Unable to recognize text in the image");
                
                // Берем все страницы из OCR (может быть больше одной страницы). 
                var pages = textProcessor.ProcessTextDocument(recognizedText);
                foreach (var page in pages.Pages)
                {
                    var layoutResult = await _gptLayout.LayoutParsingByLLM2(page, currentInvoice);

                    if ( layoutResult == null)
                        throw new Exception($"Text recognition error: Unable to recognize text in the image");

                    // Добавляем страницу с текстом в общий документ
                    allPagesData.Pages.Add(layoutResult);
                }


            }

            var productTable = new TablesDataPage();
            productTable.ProductHeaders = allPagesData.Pages.SelectMany(p => p.ProductHeaders).Distinct().ToList();
            productTable.ProductRows = allPagesData.Pages.SelectMany(p => p.ProductRows).ToList();
            productTable.TaxCategoriesHeaders = allPagesData.Pages.SelectMany(p => p.TaxCategoriesHeaders).Distinct().ToList();
            productTable.TaxCategoriesRows = allPagesData.Pages.SelectMany(p => p.TaxCategoriesRows).ToList();


            // Парсинг таблицы продуктов
            currentInvoice = await _gptParser.ReceiptParsingByLLM (productTable, currentInvoice, measUnits);
            if (currentInvoice.Products == null || currentInvoice.Products.Count() == 0)
                throw new Exception($"Text recognition error: Product list text parsing error");

            return currentInvoice;
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