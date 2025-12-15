using OpenAI.VectorStores;
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
        Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages, string vectorStoreId);

    }

    public class GPTMappingService : IGPTMappingService
    {

        private readonly GPTMappingEnvironment _env;

        public GPTMappingService()
        {
            _env = new GPTMappingEnvironment();
        }



        public async Task<List<MatchedInvoiceProduct>> ReceiptMappingByLLM(InvoiceDTO currentInvoice, ConnectionParameterDTO conParam, List<RMSMeasureUnitDTO> measUnits, List<RMSAccountDTO> storages, string vectorStoreId)
        {
            try
            {
                // Конфигурация HTTP-клиента
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(5) // Increase timeout to 5 minutes
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _env.GetApiKey());

                //============================================================================
                // MAPPING STAGE
                //============================================================================

                var mappingJsonBody = _env.GetMappingRequestBody(currentInvoice, conParam, measUnits, storages, vectorStoreId);
                // Сериализация тела запроса
                var mappingHttpContent = new StringContent(mappingJsonBody, Encoding.UTF8, "application/json");
                Console.WriteLine($"Sending request to OpenAI API:..{mappingHttpContent.ToString()}");

                // Отправка POST-запроса
                var mappingResponse = await httpClient.PostAsync(_env.GetURL(), mappingHttpContent);

                // Проверка ответа
                if (!mappingResponse.IsSuccessStatusCode)
                {
                    var errorContent = await mappingResponse.Content.ReadAsStringAsync();

                    throw new Exception($"OpenAI API error: {errorContent}");
                }
                // Чтение и возврат результата
                var mappingResponseContent = await mappingResponse.Content.ReadAsStringAsync();
                var mappingResult = ResponseParsing(mappingResponseContent);

                // берем все замепленные Id продуктов
                var mappingMappedIds = mappingResult
                .Where(x => x?.RMSProduct != null && x.InvoiceProduct != null)
                .Select(x => x.InvoiceProduct.Id)
                .ToHashSet();

                // и проверяем, есть ли ещё не замепленные продукты
                var mappingUnmapped = currentInvoice.Products
                .Where(p => !mappingMappedIds.Contains(p.Id))
                .ToList();


                if (!mappingUnmapped.Any())
                    return mappingResult;


                //============================================================================
                // PRODUCT STAGE
                //============================================================================

                var productJsonBody = _env.GetProductRequestBody(currentInvoice, mappingUnmapped, conParam, measUnits, storages, vectorStoreId);
                // Сериализация тела запроса
                var productHttpContent = new StringContent(productJsonBody, Encoding.UTF8, "application/json");
                Console.WriteLine($"Sending request to OpenAI API:..{productHttpContent.ToString()}");

                // Отправка POST-запроса
                var productResponse = await httpClient.PostAsync(_env.GetURL(), productHttpContent);

                // Проверка ответа
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();

                    throw new Exception($"OpenAI API error: {errorContent}");
                }
                // Чтение и возврат результата
                var productResponseContent = await productResponse.Content.ReadAsStringAsync();
                var productResult = ResponseParsing(productResponseContent);


                var mergedResult = mappingResult
                    .Concat(productResult)
                    .ToList();

                return mergedResult;

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
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // 1. Берём массив output
                if (!root.TryGetProperty("output", out var outputArray) ||
                    outputArray.ValueKind != JsonValueKind.Array ||
                    outputArray.GetArrayLength() == 0)
                {
                    throw new Exception("OpenAI response does not contain non-empty 'output' array.");
                }

                // 2. Ищем объект типа message
                JsonElement? messageElement = null;
                foreach (var item in outputArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var typeEl) &&
                        typeEl.ValueKind == JsonValueKind.String &&
                        typeEl.GetString() == "message")
                    {
                        messageElement = item;
                        break;
                    }
                }

                if (messageElement is null)
                    throw new Exception("OpenAI response does not contain 'message' output.");

                var msg = messageElement.Value;

                if (!msg.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array ||
                    contentArray.GetArrayLength() == 0)
                {
                    throw new Exception("OpenAI response 'message' does not contain non-empty 'content' array.");
                }

                // 3. Внутри content ищем блок с type == "output_text"
                JsonElement? outputTextElement = null;
                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var ctType) &&
                        ctType.ValueKind == JsonValueKind.String &&
                        ctType.GetString() == "output_text")
                    {
                        outputTextElement = contentItem;
                        break;
                    }
                }

                if (outputTextElement is null)
                    throw new Exception("OpenAI response 'content' does not contain 'output_text' item.");

                var ot = outputTextElement.Value;

                if (!ot.TryGetProperty("text", out var textElement))
                {
                    throw new Exception("OpenAI 'output_text' does not contain 'text' field.");
                }

                string? jsonPayload;

                if (textElement.ValueKind == JsonValueKind.String)
                {
                    // текущий формат Response API — text сразу строка с JSON
                    jsonPayload = textElement.GetString();
                }
                else if (textElement.ValueKind == JsonValueKind.Object &&
                         textElement.TryGetProperty("text", out var innerText) &&
                         innerText.ValueKind == JsonValueKind.String)
                {
                    // запасной вариант, если когда-то будет обёртка { "text": "..." }
                    jsonPayload = innerText.GetString();
                }
                else
                {
                    throw new Exception("OpenAI 'output_text.text' is not a string.");
                }

                if (string.IsNullOrWhiteSpace(jsonPayload))
                {
                    throw new Exception("OpenAI 'output_text.text' is null or empty.");
                }

                // 4. Парсим JSON, который вернула модель (это уже наш payload по схеме)
                using var payloadDoc = JsonDocument.Parse(jsonPayload);
                var payloadRoot = payloadDoc.RootElement;

                if (!payloadRoot.TryGetProperty("MatchedInvoiceProducts", out var matchedArray) ||
                    matchedArray.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("Parsed payload JSON does not contain 'MatchedInvoiceProducts' array.");
                }

                var options = JsonHelper.GetSerializerOptions();

                // 5. Десериализуем массив в ваш тип-список
                var list = JsonSerializer.Deserialize<MatchedInvoiceProducts>(
                               matchedArray.GetRawText(), // только массив
                               options)
                           ?? new MatchedInvoiceProducts();

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to MatchedInvoiceProducts object", ex);
            }
        }




        private MatchedInvoiceProducts ResponseParsingOld(string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // CHANGED: схема ответа Responses, а не ChatCompletions
                // Ожидаем:
                // output[0].content[0].text – строка с JSON по JSON Schema
                if (!root.TryGetProperty("output", out var outputArray) ||
                    outputArray.ValueKind != JsonValueKind.Array ||
                    outputArray.GetArrayLength() == 0)
                {
                    throw new Exception("No output in Responses API response");
                }

                var firstOutput = outputArray[0];

                if (!firstOutput.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array ||
                    contentArray.GetArrayLength() == 0)
                {
                    throw new Exception("No content in Responses API output");
                }

                var firstContent = contentArray[0];

                if (!firstContent.TryGetProperty("text", out var textElement) ||
                    !textElement.TryGetProperty("text", out var innerTextElement))
                {
                    throw new Exception("Structured text not found in Responses API output");
                }

                var jsonText = innerTextElement.GetString();
                if (string.IsNullOrWhiteSpace(jsonText))
                    throw new Exception("Empty JSON text in Responses API output");

                using var contentDoc = JsonDocument.Parse(jsonText);
                var rootContent = contentDoc.RootElement;

                if (!rootContent.TryGetProperty("MatchedInvoiceProducts", out _))
                {
                    throw new Exception("MatchedInvoiceProducts property not found in structured JSON");
                }

                var options = JsonHelper.GetSerializerOptions();
                var matched = JsonSerializer.Deserialize<MatchedInvoiceProducts>(rootContent, options);

                return matched ?? new MatchedInvoiceProducts();
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing Responses API structured JSON", ex);
            }
        }


        private MatchedInvoiceProducts ResponseParsingOld2(string responseContent)
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
                        var matchedProducts = JsonSerializer.Deserialize<MatchedInvoiceProducts>(contentElement, options);

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


