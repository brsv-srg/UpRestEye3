using Google.Cloud.Vision.V1;
using System.Drawing;
using System.Text;
using UpRestEye3.Models;


namespace UpRestEye3.Services
{
 
    public interface ITextRecognition
    {
        Task<RecognizedDocument> TextRecognize(Bitmap sourceImage);
    }

    public class TextRecognitionGoogleVision : ITextRecognition
    {

        public TextRecognitionGoogleVision()
        {
            // загрузка кредов TODO: вынести в конфиг
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", ".\\Properties\\VisionAccount.json");
        }

       

        public async Task<RecognizedDocument> TextRecognize(Bitmap sourceImage)
        {
            var googleImage = Google.Cloud.Vision.V1.Image.FromBytes(BitmapToBytes(sourceImage));


            var clientIA = await ImageAnnotatorClient.CreateAsync();
            TextAnnotation text = clientIA.DetectDocumentText(googleImage);
            Console.WriteLine($"Text: {text.Text}");

            var jsonObject = new RecognizedDocument();
            int blockIndex = 0;


            var recognizedDocument = new RecognizedDocument();
            int blockNumber = 0;
            foreach (Page page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    var textBlock = new TextBlock
                    {
                        BlockNumber = blockNumber++,
                        BlockCoordinates = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
                        Paragraphs = new List<TextParagraph>()
                    };
                    int paragraphNumber = 0;
                    foreach (var paragraph in block.Paragraphs)
                    {
                        var paragraphText = new StringBuilder();
                        foreach (var word in paragraph.Words)
                        {
                            paragraphText.Append(string.Join("", word.Symbols.Select(s => s.Text))).Append(" ");
                        }

                        textBlock.Paragraphs.Add(new TextParagraph
                        {
                            ParagraphNumber = paragraphNumber++,
                            ParagraphCoordinates = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})")),
                            ParagraphText = paragraphText.ToString()
                        });
                    }

                    recognizedDocument.TextBlocks.Add(textBlock);
                }
            }

            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(recognizedDocument));

            return recognizedDocument;

        }


        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
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
