using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using UpRestEye3.MLImageModels;
using Google.Cloud.Vision.V1;
using Microsoft.VisualBasic;
using System.Buffers.Text;
using System.Data;
using System.IO.Pipelines;
using System.Text.RegularExpressions;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{

    public interface IGPTService
    {
        Task<Invoice> ParseReceiptWithLLM(string ocrText);

    }

    //Класс создает и обучает модель машинного обучения
    public class GPTService : IGPTService
    {
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";

        public async Task<Invoice> ParseReceiptWithLLM(string ocrText)
        {
            // URL API OpenAI
            string url = "https://api.openai.com/v1/chat/completions";
            string consumerNIF = "515409723";

            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4",
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant that structures OCR data into JSON." },
                    new { role = "user", content = $@"
                    Extract structured data from this receipt. 
                    1. Discard the unimportant characters and unnecessary information, leaving only the important data. 
                    2. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
                    3. Identify the seller (supplier), put his name and tax number and, if an IBAN bank account is found, in JSON in the Supplier section. 
                    4. Define the buyer, sometimes it is only a TAX-ID (NIF) and it should match this NIF: {consumerNIF}. Put the NIF and the name of the buyer in JSON in the Consumer section.
                    5. Determine the invoice number and date, put them in JSON in the Info section.
                    6. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
                    7. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
                    6. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
                    7. Verify that the sum of the items in the Products list is equal to the sum in the Total section. 
                    8. The structure of the output result should be as follows:
                    {{
                      ""Id"": 0,
                      ""Supplier"": {{
                        ""Name"": ""string"",
                        ""TaxNumber"": ""string"",
                        ""BankAccount"": ""string""
                      }},
                      ""Consumer"": {{
                        ""Name"": ""string"",
                        ""TaxNumber"": ""string""
                      }},
                      ""Info"": {{
                        ""InvoiceNumber"": ""string"",
                        ""InvoiceDate"": ""2023-10-10T00:00:00"",
                        ""TotalAmountInclTaxes"": 0.0,
                        ""TotalAmountExclTaxes"": 0.0
                      }},
                      ""Products"": [
                        {{
                          ""Id"": 0,
                          ""ProductCode"": ""string"",
                          ""ProductName"": ""string"",
                          ""Unit"": ""string"",
                          ""Quantity"": 0.0,
                          ""Price"": 0.0
                        }}
                      ],
                      ""TaxCategories"": [
                        {{
                          ""Id"": 0,
                          ""Category"": ""string"",
                          ""Amount"": 0.0
                        }}
                      ],
                      ""FilePath"": ""string"",
                      ""UploadTime"": ""2023-10-10T00:00:00""
                    }}
                    ", consumerNIF }
                },
                temperature = 0.2
            };

            // Сериализация тела запроса
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Конфигурация HTTP-клиента
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Отправка POST-запроса
            var response = await httpClient.PostAsync(url, httpContent);

            // Проверка ответа
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {errorContent}");
            }

            // Чтение и возврат результата
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseContent);
            

            try
            {
                var invoice = JsonSerializer.Deserialize<Invoice>(result);
                return invoice;
            }
            catch (JsonException ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }
    }






}
