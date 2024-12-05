using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Net.Http;
using UpRestEye3.NewServices;
using UpRestEye3.Services;


namespace UpRestEye3.NewServices
{
    class GoogleVisionOCR
    {

        public GoogleVisionOCR()
        {
            // загрузка кредов TODO: вынести в конфиг
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", ".\\Properties\\GInvoiceRecognition.json");
        }

        public async Task<List<string>> RecognizeAI(byte[] image)
        {

            var client = ImageAnnotatorClient.Create();
            var responseContent = await client.DetectTextAsync(Image.FromBytes(image));

            var res = new List<string>();

            // Вывод результатов
            foreach (var annotation in responseContent)
            {
                res.Add(annotation.Description);
                Console.WriteLine($"Распознанный текст: {annotation.Description}");
            }

            return res;

        }
    }
}
