using Google.Cloud.DocumentAI.V1;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UpRestEye3.Models.BLO;
using static Google.Rpc.Context.AttributeContext.Types;


namespace UpRestEye3.Services.Recognition
{

    public interface ITextRecognition
    {
        
        Task<string> DocumentRecognize(Bitmap sourceImage);
        Task<SimplifiedDocument> TextRecognize(Bitmap sourceImages);
        Task<QRCodeData> QRRecognize(Bitmap sourceImage);

    }

    public class TextRecognitionGoogleVision : ITextRecognition
    {

        public TextRecognitionGoogleVision()
        {
            // загрузка кредов TODO: вынести в конфиг
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", ".\\Properties\\VisionAccount.json");
        }


        public async Task<SimplifiedDocument> TextRecognize(Bitmap sourceImage)
        {
            // TODO тут поправить

            SimplifiedDocument simplifiedDocument = null;

            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(BitmapToBytes(sourceImage));

            var clientIA = await ImageAnnotatorClient.CreateAsync();
            TextAnnotation text = clientIA.DetectDocumentText(googleImage);
            Console.WriteLine($"Text: {text.Text}");

            //HtmlGenerator htmlGenerator = new HtmlGenerator();
            //var html = htmlGenerator.GenerateHtmlFromTextAnnotation(text);
            //htmlGenerator.SaveHtmlToFile(html, "hhttmmllTextAnnotation.html");

            // Упрощенная структура для сериализации
            simplifiedDocument = new SimplifiedDocument
            {
                //Text = text.Text,
                Pages = text.Pages.Select(page => new SimplifiedPage
                {
                    Blocks = page.Blocks.Select(block => new SimplifiedBlock
                    {
                        BlockCoordinates = ConvertBoundingPolyToRectangleCoordinates(block.BoundingBox),
                        Paragraphs = block.Paragraphs.Select(paragraph => new SimplifiedParagraph
                        {
                            ParagraphCoordinates = ConvertBoundingPolyToRectangleCoordinates(paragraph.BoundingBox),
                            Words = paragraph.Words.Select(word => new SimplifiedWord
                            {
                                WordText = string.Join("", word.Symbols.Select(s => s.Text)),
                                WordCoordinates = ConvertBoundingPolyToRectangleCoordinates(word.BoundingBox)
                            }).ToList()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return simplifiedDocument;
        }

 
        public async Task<QRCodeData> QRRecognize(Bitmap sourceImage)
        {
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(BitmapToBytes(sourceImage));

            var clientIA = await ImageAnnotatorClient.CreateAsync();
            var text = await clientIA.DetectTextAsync(googleImage);
           

            if (text == null || text.Count == 0)
                return null;

            // Сконкатенируем все фрагменты текста
            string allText = string.Join("\n", text.Select(x => x.Description));

            // Определяем регулярное выражение для поиска строки в формате QR
            // Пример: A:510945929*B:514092815*C:PT*D:FT*...
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";

            var match = Regex.Match(allText, pattern);
            if (match.Success)
            {
                // Если нашли совпадение, создаем объект QRCodeData
                var qrCodeData = new QRCodeData(match.Value.Trim());
                return qrCodeData;
            }

            return null;
        }

 


        public async Task<string> DocumentRecognize(Bitmap sourceImage)
        {


            //string projectId = "ai-vision-api-443817";  // Укажите ID вашего проекта
            string projectId = "140535166884";  // Укажите ID вашего проекта
            
            string locationId = "eu";  // Выберите регион (us, eu, asia)
            string processorId = "3901fcb77560ecb5";  // ID Document AI Processor (создается в Google Cloud)




            // Create client
            var client = new DocumentProcessorServiceClientBuilder
            {
                Endpoint = $"{locationId}-documentai.googleapis.com"
            }.Build();

            // Read in local file
            ByteString imageBytes = ByteString.CopyFrom(BitmapToBytes(sourceImage));
            var rawDocument = new RawDocument
            {
                Content = imageBytes,
                MimeType = GetMimeType(sourceImage)
            };


            // Initialize request argument(s)
            var request = new ProcessRequest
            {
                Name = ProcessorName.FromProjectLocationProcessor(projectId, locationId, processorId).ToString(),
                RawDocument = rawDocument
            };

            // Make the request
            var response = await client.ProcessDocumentAsync(request);

            var document = response.Document;

            // Преобразуем данные в объект
            var invoiceData = ExtractInvoiceData(document);

            // Сериализация в JSON
            return JsonConvert.SerializeObject(invoiceData, Formatting.Indented);
        }


        public static Dictionary<string, object> ExtractInvoiceData(Document document)
        {
            Dictionary<string, object> invoiceData = new Dictionary<string, object>();

            // Извлекаем основную информацию
            invoiceData["text"] = document.Text;
            invoiceData["entities"] = new List<Dictionary<string, string>>();
            invoiceData["tables"] = new List<List<List<string>>>(); // Таблицы, если есть

            // Разбираем извлеченные сущности (номер инвойса, сумма, дата и т.д.)
            foreach (var entity in document.Entities)
            {
                var entityData = new Dictionary<string, string>
            {
                { "type", entity.Type },
                { "value", entity.MentionText }
            };
                ((List<Dictionary<string, string>>)invoiceData["entities"]).Add(entityData);
            }

            // Извлекаем табличные данные (если есть)
            foreach (var page in document.Pages)
            {
                foreach (var table in page.Tables)
                {
                    var tableData = new List<List<string>>();
                    foreach (var row in table.BodyRows)
                    {
                        var rowData = new List<string>();
                        foreach (var cell in row.Cells)
                        {
                            rowData.Add(cell.Layout.TextAnchor.Content);
                        }
                        tableData.Add(rowData);
                    }
                    ((List<List<List<string>>>)invoiceData["tables"]).Add(tableData);
                }
            }

            return invoiceData;
        }



        private RectangleCoordinates ConvertBoundingPolyToRectangleCoordinates(Google.Cloud.Vision.V1.BoundingPoly boundingPoly)
        {
            return new RectangleCoordinates
            {
                TopLeft = new TPoint(boundingPoly.Vertices[0].X, boundingPoly.Vertices[0].Y),
                TopRight = new TPoint(boundingPoly.Vertices[1].X, boundingPoly.Vertices[1].Y),
                BottomRight = new TPoint(boundingPoly.Vertices[2].X, boundingPoly.Vertices[2].Y),
                BottomLeft = new TPoint(boundingPoly.Vertices[3].X, boundingPoly.Vertices[3].Y)
            };
        }

        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }


        public string GetMimeType(Bitmap bitmap)
         {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            ImageFormat format = bitmap.RawFormat;
            string mimeType = "image/unknown";

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    mimeType = codec.MimeType;
                    break;
                }
            }

            return mimeType.ToLower();
        }


        //public async Task<RecognizedDocument> RecognizeAI(Bitmap sourceImage)
        //{

        //    var client = ImageAnnotatorClient.Create();
        //    var responseContent = await client.DetectTextAsync(Image.FromBytes(image));

        //    var res = new List<string>();

        //    // Вывод результатов
        //    foreach (var annotation in responseContent)
        //    {
        //        res.Add(annotation.Description);
        //        Console.WriteLine($"Распознанный текст: {annotation.Description}");
        //    }

        //    return res;
        //}
}
}






//def preprocess_ocr_results(ocr_results):
//    """
//    Предварительная обработка OCR результатов для улучшения группировки.

//    Args:
//        ocr_results: Исходные результаты OCR

//    Returns:
//        Обработанные результаты OCR
//    """
//    # Сортировка блоков по Y-координате для лучшего понимания LLM
//    ocr_results = sorted(ocr_results, key = lambda x: (x['y1'] + x['y2']) / 2)

//    # Рассчитаем средний размер блока для оценки искажений
//    avg_height = sum([(x['y2'] - x['y1']) for x in ocr_results]) / len(ocr_results)

//    # Добавим метаданные о блоках
//    for block in ocr_results:
//        # Рассчитаем центр блока
//        block['center_x'] = (block['x1'] + block['x2']) / 2
//        block['center_y'] = (block['y1'] + block['y2']) / 2

//        # Рассчитаем размеры блока
//        block['width'] = block['x2'] - block['x1']
//        block['height'] = block['y2'] - block['y1']

//        # Определим, является ли блок числом или текстом
//        if block['text'].replace('.', '').replace(',', '').isdigit():
//            block['type'] = 'number'
//        else:
//            block['type'] = 'text'


//    return ocr_results



//public async Task<RecognizedDocument> TextRecognize(Bitmap sourceImage)
//{
//    var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(BitmapToBytes(sourceImage));


//    var clientIA = await ImageAnnotatorClient.CreateAsync();
//    TextAnnotation text = clientIA.DetectDocumentText(googleImage);
//    Console.WriteLine($"Text: {text.Text}");

//    var jsonObject = new RecognizedDocument();
//    int blockIndex = 0;



//    var recognizedDocument = new RecognizedDocument();
//    int blockNumber = 0;
//    foreach (Page page in text.Pages)
//    {
//        foreach (var block in page.Blocks)
//        {
//            var textBlock = new TextBlock
//            {
//                BlockNumber = blockNumber++,
//                BlockCoordinates = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
//                Paragraphs = new List<TextParagraph>()
//            };
//            int paragraphNumber = 0;
//            foreach (var paragraph in block.Paragraphs)
//            {
//                var paragraphText = new StringBuilder();
//                foreach (var word in paragraph.Words)
//                {
//                    paragraphText.Append(string.Join("", word.Symbols.Select(s => s.Text))).Append(" ");
//                }

//                textBlock.Paragraphs.Add(new TextParagraph
//                {
//                    ParagraphNumber = paragraphNumber++,
//                    ParagraphCoordinates = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
//                    ParagraphText = paragraphText.ToString()
//                });
//            }

//            recognizedDocument.TextBlocks.Add(textBlock);
//        }
//    }

//    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(recognizedDocument));

//    return recognizedDocument;

//}




//public async Task<string> TextRecognize2(Bitmap sourceImage)
//{
//    var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(BitmapToBytes(sourceImage));

//    var clientIA = await ImageAnnotatorClient.CreateAsync();
//    TextAnnotation text = clientIA.DetectDocumentText(googleImage);
//    Console.WriteLine($"Text: {text.Text}");

//    // Упрощенная структура для сериализации
//    var simplifiedDocument = new
//    {
//        //Text = text.Text,
//        Pages = text.Pages.Select(page => new
//        {
//            Blocks = page.Blocks.Select(block => new
//            {
//                BlockCoordinates = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
//                Paragraphs = block.Paragraphs.Select(paragraph => new
//                {
//                    ParagraphCoordinates = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
//                    //ParagraphText = string.Join(" ", paragraph.Words.Select(word => string.Join("", word.Symbols.Select(s => s.Text))))
//                    Words = paragraph.Words.Select(word => new
//                    {
//                        WordText = string.Join("", word.Symbols.Select(s => s.Text)),
//                        WordCoordinates = string.Join(" - ", word.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"))
//                    })
//                })
//            })
//        })
//    };

//    // Сериализация упрощенной структуры
//    var options = new JsonSerializerOptions { WriteIndented = true };
//    string serializedDocument = System.Text.Json.JsonSerializer.Serialize(simplifiedDocument, options);

//    return serializedDocument;
//}
