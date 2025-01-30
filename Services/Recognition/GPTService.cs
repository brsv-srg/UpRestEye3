using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.Recognition
{

    public interface IGPTService
    {
        Task<InvoiceDTO> ParseReceiptWithLLM(RecognizedDocument invoiceText, InvoiceDTO currentInvoice);

    }

    public class GPTService : IGPTService
    {
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";

        private readonly GPTEnvironment _env;

        public GPTService()
        {
            _env = new GPTEnvironment();
        }

        public async Task<InvoiceDTO> ParseReceiptWithLLM(RecognizedDocument invoiceText, InvoiceDTO currentInvoice)
        {
            // URL API OpenAI
            // TODO Убрать URL в параметры 

            string url = "https://api.openai.com/v1/chat/completions";


            // TODO: Убрать в environment
            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", // "o1 -preview-2024-09-12",
                messages = new object[]
                {
                    new { role = "system", content = _env.GetSystemPrompt(currentInvoice) },
                    new { role = "user", content = $@"Extract structured data from this receipt: {JsonSerializer.Serialize(invoiceText)}"} // env.GetTestRequestPrompt()}" }
            },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(_env.GetResponseFormat()).RootElement
                    }
                },
                temperature = 0.1
            };

            // Сериализация тела запроса
            var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });


            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");


            // Конфигурация HTTP-клиента
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            Console.WriteLine($"Sending request to OpenAI API:..{httpContent.ToString()}");
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


            try
            {
                // Разбор JSON-ответа
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // Извлечение элемента, содержащего данные Invoice
                if (root.TryGetProperty("choices", out JsonElement choicesElement) &&
                    choicesElement[0].TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.TryGetProperty("content", out JsonElement contentElement) &&
                    contentElement.ValueKind == JsonValueKind.String)
                {
                    using var contentDocument = JsonDocument.Parse(contentElement.GetString());
                    var rootContent = contentDocument.RootElement;

                    if (rootContent.TryGetProperty("Supplier", out contentElement))
                    {
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateTimeJsonConverter(),
                                            new DecimalJsonConverter(),
                                            new IntegerJsonConverter(),
                                            new TaxCategoryJsonConverter()},
                            PropertyNameCaseInsensitive = true
                        };

                        Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                        InvoiceDTO invoice = invoiceDocument.Deserialize<InvoiceDTO>(options);
                        invoice.Status = InvoiceStatus.TextProcessed;

                        return invoice;
                    }
                    else
                    {
                        throw new Exception("Invoice element not found in JSON response");
                    }
                }
                else
                {
                    throw new Exception("Invalid JSON structure");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }
    }

}
