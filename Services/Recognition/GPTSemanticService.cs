using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;
using System.Drawing;
using UpRestEye3.Components.Pages;
using static Google.Apis.Requests.BatchRequest;

namespace UpRestEye3.Services.Recognition
{

    public interface IGPTSemanticService
    {
        Task<InvoiceDTO?> ReceiptParsingByLLM(TablesDataDocument tablesDataDocument, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits);

    }

    public class GPTSemanticService : IGPTSemanticService
    {

        private readonly GPTSemanticEnvironment _env;

        public GPTSemanticService()
        {
            _env = new GPTSemanticEnvironment();
        }

        


        public async Task<InvoiceDTO?> ReceiptParsingByLLM(TablesDataDocument tablesDataDocument, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {

            // Сериализация тела запроса
            var jsonBody = _env.GetReceiptParsingRequestBody(tablesDataDocument, currentInvoice, measUnits);


            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");


            // Конфигурация HTTP-клиента
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Increase timeout to 5 minutes
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _env.GetApiKey());
            Console.WriteLine($"Sending request to OpenAI API:..{httpContent.ToString()}");
            // Отправка POST-запроса
            var response = await httpClient.PostAsync(_env.GetURL(), httpContent);

            // Проверка ответа
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {errorContent}");
            }


            // Чтение и возврат результата
            var responseContent = await response.Content.ReadAsStringAsync();

            currentInvoice.Products = ResponseInvoiceParsing(responseContent);

            return currentInvoice;
        }


        //todo поправить с датой загрузки 
        private List<InvoiceProductDTO> ResponseInvoiceParsing(string responseContent)
        {
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

                    if (rootContent.TryGetProperty("Products", out contentElement))
                    {
                        var options = JsonHelper.GetSerializerOptions();

                        Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                        InvoiceDTO invoice = invoiceDocument.Deserialize<InvoiceDTO>(options);

                        return invoice.Products;
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
