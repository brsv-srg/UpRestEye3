using System.Drawing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using static Google.Apis.Requests.BatchRequest;

namespace UpRestEye3.Services.Recognition
{

    public interface IGPTMappingService
    {
        Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages);

    }

    public class GPTMappingService : IGPTMappingService
    {

        private readonly GPTMappingEnvironment _env;

        public GPTMappingService()
        {
            _env = new GPTMappingEnvironment();
        }



        public async Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages)
        {

            try
            {
                var jsonBody = _env.GetReceiptMappingRequestBody2(currentInvoice, supplierProducts, conParam, measUnits, storages);


                // Сериализация тела запроса
                var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Конфигурация HTTP-клиента
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(5) // Increase timeout to 5 minutes
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _env.GetApiKey());

                Console.WriteLine($"Sending request to OpenAI API:..{httpContent.ToString()}");

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    // Отправка POST-запроса
                    var response = await httpClient.PostAsync(_env.GetURL(), httpContent);

                    // Проверка ответа
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
                        {
                            var retryAfter = ExtractRetryAfterSeconds(errorContent);
                            if (!retryAfter.HasValue)
                                retryAfter = 30;

                            Console.WriteLine($"Rate limit exceeded. Retrying after {retryAfter.Value} seconds.");
                            await Task.Delay(retryAfter.Value * 1000);
                            continue;
                        }

                        throw new Exception($"OpenAI API error: {errorContent}");
                    }
                    else
                    {
                        var options = JsonHelper.GetSerializerOptions();

                        // Чтение и возврат результата
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = ResponseParsing(responseContent);

                        return result;
                    }
                }

                return new List<MatchedInvoiceProduct>();

            }
            catch (Exception ex)
            {
                currentInvoice.StageStatus = InvoiceStatusEnum.Error;

                throw new Exception("Error mapping invoice products to RMS products", ex);
            }
        }

        private int? ExtractRetryAfterSeconds(string errorContent)
        {
            try
            {
                using var document = JsonDocument.Parse(errorContent);
                var root = document.RootElement;

                if (root.TryGetProperty("error", out JsonElement errorElement) &&
                    errorElement.TryGetProperty("message", out JsonElement messageElement))
                {
                    var message = messageElement.GetString();
                    var match = Regex.Match(message, @"Please try again in (\d+(\.\d+)?)s", RegexOptions.IgnoreCase);
                    if (match.Success && double.TryParse(match.Groups[1].Value, out double retryAfter))
                    {
                        return (int)Math.Ceiling(retryAfter);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing retry-after seconds: {ex.Message}");
            }

            return null;
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


