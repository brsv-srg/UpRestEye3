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

    public interface IGPTService
    {
        Task<InvoiceDTO?> ReceiptParsingByLLM(RecognizedDocument invoiceText, InvoiceDTO currentInvoice);
        Task<InvoiceDTO?> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts);

    }

    public class GPTService : IGPTService
    {

        private readonly GPTEnvironment _env;

        public GPTService()
        {
            _env = new GPTEnvironment();
        }

        public async Task<InvoiceDTO?> ReceiptParsingByLLM(RecognizedDocument invoiceText, InvoiceDTO currentInvoice)
        {

            // Сериализация тела запроса
            var jsonBody = _env.GetReceiptParsingRequestBody(invoiceText, currentInvoice);


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


            // Чтение и возврат результата
            var responseContent = await response.Content.ReadAsStringAsync();

            var invoice = ResponseInvoiceParsing(responseContent);

            if (invoice != null)
                invoice.Status = InvoiceStatusEnum.TextProcessed;

            return invoice;
        }

        public async Task<InvoiceDTO?> ReceiptMappingByLLM(InvoiceDTO currentInvoice, List<RMSProductDTO> supplierProducts)
        {

            var jsonBody = _env.GetReceiptMappingRequestBody(currentInvoice, supplierProducts);


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


            // Чтение и возврат результата
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = ResponseInvoiceAndRMSProductsParsing(responseContent);
            
            
            return result;

        }

        //todo поправить с датой загрузки 
        private InvoiceDTO? ResponseInvoiceParsing(string responseContent)
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

                    if (rootContent.TryGetProperty("Supplier", out contentElement))
                    {
                        var options = JsonHelper.GetSerializerOptions();

                        Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                        InvoiceDTO invoice = invoiceDocument.Deserialize<InvoiceDTO>(options);

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

        private InvoiceDTO ResponseInvoiceAndRMSProductsParsing(string responseContent)
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

                    if (rootContent.TryGetProperty("Invoice", out contentElement))
                    {
                        var options = JsonHelper.GetSerializerOptions();

                        Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                        var mappedInvoice = invoiceDocument.Deserialize<InvoiceDTO>(options);

                        return mappedInvoice;
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

        /*{
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                InvoiceDTO? invoice = null;
                List<RMSProductDTO>? rmsProducts = [];
                var options = new JsonSerializerOptions
                {
                    Converters = { new DateTimeJsonConverter(),
                                       new DecimalJsonConverter(),
                                       new IntegerJsonConverter(),
                                       new TaxCategoryJsonConverter(),
                                       new RMSProductStateJsonConverter() },
                    PropertyNameCaseInsensitive = true
                };

                // Разбор JSON-ответа
                if (root.TryGetProperty("choices", out JsonElement choicesElement) &&
                    choicesElement[0].TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.TryGetProperty("content", out JsonElement contentElement) &&
                    contentElement.ValueKind == JsonValueKind.String)
                {
                    ////////////////////////////////////////////

                    using var invoiceDocument = JsonDocument.Parse(contentElement.GetRawText());
                    JsonElement invoiceRootElement = invoiceDocument.RootElement;


                    // Проверяем, является ли Invoice строкой или объектом
                    if (invoiceRootElement.ValueKind == JsonValueKind.String)
                    {

                        // Получаем строку JSON
                        string jsonContent = invoiceRootElement.GetString();

                        // Парсим строку как JSON
                        using JsonDocument innerDoc = JsonDocument.Parse(jsonContent);

                        // Извлекаем объект Invoice
                        //JsonElement invoiceElement = 
                        innerDoc.RootElement.TryGetProperty("Invoice",out var invoiceElement);

                        invoice = JsonSerializer.Deserialize<InvoiceDTO>(invoiceElement);

                    }
                    else if (invoiceRootElement.ValueKind == JsonValueKind.Object)
                    {
                        // Если это объект, десериализуем напрямую
                        invoice = JsonSerializer.Deserialize<InvoiceDTO>(invoiceRootElement.GetRawText());
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected data format for Invoice.");
                    }


                    ////////////////////////////////////////////

                    // Десериализация объекта Invoice

                    /*var invoiceElement = contentElement.GetProperty("Invoice");

                    if (invoiceElement.ValueKind == JsonValueKind.String)
                    {
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateTimeJsonConverter(),
                                       new DecimalJsonConverter(),
                                       new IntegerJsonConverter(),
                                       new TaxCategoryJsonConverter(),
                                       new RMSProductStateJsonConverter() },
                            PropertyNameCaseInsensitive = true
                        };

                        invoice = JsonSerializer.Deserialize<InvoiceDTO>(invoiceElement, options);
                    }
                    else
                    {
                        throw new Exception("Invoice element not found in JSON response");
                    }
                    * /

                    if (contentElement.TryGetProperty("RMSProducts", out JsonElement rmsProductsElement))
                    {
                        
                        rmsProducts = JsonSerializer.Deserialize<List<RMSProductDTO>>(rmsProductsElement.GetRawText(), options);
                    }
                    else
                    {
                        throw new Exception("RMS Products not found in JSON response");
                    }
                }
                else
                {
                    throw new Exception("Invalid JSON structure");
                }
                return (invoice, rmsProducts);
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }*/

    }

}


/*
 
        private (InvoiceDTO?, List<RMSProductDTO>) ResponseInvoiceAndRMSProductsParsing(string responseContent)
        {
            InvoiceDTO? invoice = null;
            List<RMSProductDTO>? rmsProducts = [];
            try
            {
                // Разбор JSON-ответа
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                if (root.TryGetProperty("choices", out JsonElement choicesElement) &&
                    choicesElement[0].TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.TryGetProperty("content", out JsonElement contentElement) &&
                    contentElement.ValueKind == JsonValueKind.String)
                {

                    // Извлечение элемента, содержащего данные Invoice
                    if (contentElement.TryGetProperty("Invoice", out JsonElement invoiceElement))
                    //&&
                    //invoiceElement.ValueKind == JsonValueKind.String)

                    {
                        using var invoiceContentDocument = JsonDocument.Parse(invoiceElement.GetString());
                        var invoiceRootContent = invoiceContentDocument.RootElement;
                        
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateTimeJsonConverter(),
                                            new DecimalJsonConverter(),
                                            new IntegerJsonConverter(),
                                            new TaxCategoryJsonConverter(),
                                            new RMSProductStateJsonConverter()},
                            PropertyNameCaseInsensitive = true
                        };

                        Console.WriteLine($"Received response from OpenAI API: {invoiceRootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(invoiceRootContent.GetRawText());
                        invoice = invoiceDocument.Deserialize<InvoiceDTO>(options);

                    }
                    else
                    {
                        throw new Exception("Invoice element not found in JSON response");
                    }
                    
                    // Извлечение списка RMSProducts
                    if (contentElement.TryGetProperty("RMSProducts", out JsonElement rmsProductsElement) &&
                        rmsProductsElement.ValueKind == JsonValueKind.String)
                    {
                        using var prodContentDocument = JsonDocument.Parse(rmsProductsElement.GetString());
                        var prodRootContent = prodContentDocument.RootElement;

                        
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateTimeJsonConverter(),
                                    new DecimalJsonConverter(),
                                    new IntegerJsonConverter(),
                                    new TaxCategoryJsonConverter(),
                                    new RMSProductStateJsonConverter()},
                            PropertyNameCaseInsensitive = true
                        };

                        Console.WriteLine($"Received response from OpenAI API: {prodRootContent.GetRawText()}");

                        using var invoiceDocument = JsonDocument.Parse(prodRootContent.GetRawText());
                        rmsProducts = invoiceDocument.Deserialize<List<RMSProductDTO>>(options);
                    }
                    else
                    {
                        throw new Exception("RMS Products not found in JSON response");
                    }

                }
                else
                {
                    throw new Exception("Invalid JSON structure");
                }

                return (invoice, rmsProducts);
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }

 */
