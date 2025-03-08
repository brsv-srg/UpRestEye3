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

    public interface IGPTService2
    {
        Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages);

    }

    public class GPTService2 : IGPTService2
    {

        private readonly GPTEnvironment2 _env;

        public GPTService2()
        {
            _env = new GPTEnvironment2();
        }



        public async Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages)
        {

            try
            {
                var jsonBody = _env.GetReceiptMappingRequestBody2(currentInvoice, supplierProducts, measUnits, storages);


                // Сериализация тела запроса
                var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");


                // Конфигурация HTTP-клиента
                using var httpClient = new HttpClient();
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

                var options = JsonHelper.GetSerializerOptions();

                // Чтение и возврат результата
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = ResponseParsing(responseContent);


                return result;


            }
            catch (Exception ex)
            {
                currentInvoice.Status = InvoiceStatusEnum.Error;

                throw new Exception("Error mapping invoice products to RMS products", ex);
            }
        }



        private MatchedInvoiceProducts ResponseParsing(string responseContent)
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

                    if (rootContent.TryGetProperty("MatchedInvoiceProducts", out contentElement))
                    {
                        var options = JsonHelper.GetSerializerOptions();

                        Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                        using var matchedProductsDocument = JsonDocument.Parse(rootContent.GetRawText());
                        var matchedProducts = JsonSerializer.Deserialize<MatchedInvoiceProducts>(contentElement,options);

                        return matchedProducts ?? new MatchedInvoiceProducts();

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


