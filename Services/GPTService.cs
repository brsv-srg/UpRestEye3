using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{

    public interface IGPTService
    {
        Task<Invoice> ParseReceiptWithLLM(RecognizedDocument invoiceText);

    }

    //Класс создает и обучает модель машинного обучения
    public class GPTService : IGPTService
    {
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";

        public async Task<Invoice> ParseReceiptWithLLM(RecognizedDocument invoiceText)
        {
            // URL API OpenAI
            // TODO Убрать в параметры, а поставщика доставать из QR кода и добавлять в запрос
            string url = "https://api.openai.com/v1/chat/completions";
            var env = new GPTEnvironment(ConsumerName: "Figueiredo", ConsumerNIF: "515409723");

            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", // "o1 -preview-2024-09-12",
                messages = new object[]
                {
                    new { role = "system", content = env.GetSystemPrompt() },
                    new { role = "user", content = $@"Extract structured data from this receipt: { JsonSerializer.Serialize(invoiceText)}"} // env.GetTestRequestPrompt()}" }
            },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(env.GetResponseFormat()).RootElement
                    }
                },
                temperature = 0.1
            };

            // Сериализация тела запроса
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            
            // Конфигурация HTTP-клиента
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            Console.WriteLine($"Sending request to OpenAI API:..{httpContent}");
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
                            Converters = { new DateTimeJsonConverter(), new DecimalJsonConverter(), new IntegerJsonConverter() },
                            PropertyNameCaseInsensitive = true
                        };

                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                        Invoice invoice = JsonSerializer.Deserialize<Invoice>(invoiceDocument, options);
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
            catch (JsonException ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }
    }

}
