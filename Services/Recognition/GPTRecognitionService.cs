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

    public interface IGPTRecognitionService
    {
        Task<InvoiceDTO?> RecognitionByLLM(SimpleDocument invoiceDocument, InvoiceDTO currentInvoice);

    }

    public class GPTRecognitionService : IGPTRecognitionService
    {

        private readonly GPTRecognitionEnvironment _env;

        public GPTRecognitionService()
        {
            _env = new GPTRecognitionEnvironment();
        }
       
        public async Task<InvoiceDTO?> RecognitionByLLM(SimpleDocument invoiceDocument, InvoiceDTO currentInvoice)
        {
            try
            {
                // Сериализация тела запроса
                var jsonBody = _env.GetRecognitionRequestBody(invoiceDocument, currentInvoice);


                var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");


                // Конфигурация HTTP-клиента
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(10) // Increase timeout to 5 minutes
                };


                currentInvoice.Products = ResponseInvoiceParsing(tempResult);


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
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }

        private List<InvoiceProductDTO> ResponseInvoiceParsing(string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // ✅ NEW: Responses API структура: output[] -> message -> content[] -> output_text -> text
                if (!root.TryGetProperty("output", out var outputElement) || outputElement.ValueKind != JsonValueKind.Array)
                    throw new Exception("Invalid JSON structure: missing 'output' array");

                JsonElement? messageElement = null;
                foreach (var item in outputElement.EnumerateArray())
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
                    throw new Exception("Invalid JSON structure: 'message' item not found in 'output'");

                var msg = messageElement.Value;

                if (!msg.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                    throw new Exception("Invalid JSON structure: missing 'content' array in message");

                JsonElement? outputTextElement = null;
                foreach (var c in contentArray.EnumerateArray())
                {
                    if (c.TryGetProperty("type", out var ctType) &&
                        ctType.ValueKind == JsonValueKind.String &&
                        ctType.GetString() == "output_text")
                    {
                        outputTextElement = c;
                        break;
                    }
                }

                if (outputTextElement is null)
                    throw new Exception("Invalid JSON structure: 'output_text' item not found in message.content");

                if (!outputTextElement.Value.TryGetProperty("text", out var textElement) ||
                    textElement.ValueKind != JsonValueKind.String)
                    throw new Exception("Invalid JSON structure: missing 'text' in output_text");

                var jsonPayload = textElement.GetString();
                if (string.IsNullOrWhiteSpace(jsonPayload))
                    throw new Exception("Model output_text.text is empty");

                // Далее — ваша старая логика: text это JSON строка с Invoice
                using var contentDocument = JsonDocument.Parse(jsonPayload);
                var rootContent = contentDocument.RootElement;

                if (rootContent.TryGetProperty("Products", out _))
                {
                    var options = JsonHelper.GetSerializerOptions();

                    Console.WriteLine($"Received response from OpenAI API: {rootContent.GetRawText()}");

                    // Можно без лишнего Parse, но оставляю максимально близко к вашему коду:
                    using var invoiceDocument = JsonDocument.Parse(rootContent.GetRawText());
                    var invoice = invoiceDocument.Deserialize<InvoiceDTO>(options);

                    if (invoice?.Products == null)
                        throw new Exception("Invoice.Products is null after десериализации");

                    return invoice.Products;
                }

                throw new Exception("Products element not found in JSON response");
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing JSON response to Invoice object", ex);
            }
        }

        //todo поправить с датой загрузки 
        private List<InvoiceProductDTO> ResponseInvoiceParsingOld(string responseContent)
        {
            try
            {
                // Разбор JSON-ответа
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // Извлечение элемента, содержащего данные Invoice
                if (root.TryGetProperty("output", out JsonElement choicesElement))
                {
                    if (choicesElement[0].TryGetProperty("content", out JsonElement messageElement))
                    {
                        if (messageElement.TryGetProperty("text", out JsonElement contentElement) &&
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
                
                    else
                    {
                        throw new Exception("Invalid JSON structure");
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




        private static string tempResult = $@"{{
  ""id"": ""resp_0f0529ce4465736d00694512017550819aa04c8a832deadaf3"",
  ""object"": ""response"",
  ""created_at"": 1766134273,
  ""status"": ""completed"",
  ""background"": false,
  ""billing"": {{
    ""payer"": ""developer""
  }},
  ""completed_at"": 1766134349,
  ""error"": null,
  ""incomplete_details"": null,
  ""instructions"": null,
  ""max_output_tokens"": null,
  ""max_tool_calls"": null,
  ""model"": ""gpt-5.1-2025-11-13"",
  ""output"": [
    {{
      ""id"": ""rs_0f0529ce4465736d0069451204feb4819ab6de7e63c8862f05"",
      ""type"": ""reasoning"",
      ""summary"": []
    }},
    {{
      ""id"": ""msg_0f0529ce4465736d006945122e13a0819abc478d67498820f3"",
      ""type"": ""message"",
      ""status"": ""completed"",
      ""content"": [
        {{
          ""type"": ""output_text"",
          ""annotations"": [],
          ""logprobs"": [],
          ""text"": ""{{\""Products\"":[\n  {{\""Id\"":null,\""ProductCode\"":\""5601660997072\"",\""ProductName\"":\""MPRO TOAL MAO ZZ 2F 23X21 6X20\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":9.78,\""TaxCategory\"":\""Normal\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601180000993\"",\""ProductName\"":\""LT PAST GORDO 1LT VIGOR\"",\""Unit\"":\""unit\"",\""Quantity\"":12,\""Container\"":\""CA\"",\""Count\"":1,\""ProductTotalValue\"":11.76,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660983648\"",\""ProductName\"":\""MC LT UHT GORDO ACORES 1LT * 6\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BX\"",\""Count\"":6,\""ProductTotalValue\"":4.93,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""7394376616709\"",\""ProductName\"":\""BEB AVEIA BARISTA 1LT OATLY\"",\""Unit\"":\""unit\"",\""Quantity\"":6,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":11.94,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5603722505607\"",\""ProductName\"":\""LT S/ LACTOSE MG 1LT * 6 MIMOSA\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":6,\""ProductTotalValue\"":6.12,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8410379941002\"",\""ProductName\"":\""NATA UHT 35 % MG 1L R PICOT\"",\""Unit\"":\""unit\"",\""Quantity\"":3,\""Container\"":\""CA\"",\""Count\"":1,\""ProductTotalValue\"":11.76,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5604550120147\"",\""ProductName\"":\""GEMA OVO LIQUIDA 1 KG \\\""DOVO\\\""\"",\""Unit\"":\""kg\"",\""Quantity\"":2,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":16.3,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660997201\"",\""ProductName\"":\""ARO QJ MOZZARELA FATIAS 1 KG\"",\""Unit\"":\""kg\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":7.29,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""2880789040002\"",\""ProductName\"":\""MC ABACATE HASS P / COM I > 236G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""\"",\""Count\"":1,\""ProductTotalValue\"":17.92,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660945042\"",\""ProductName\"":\""MC BAT ASSAR BRAN\u00c7A I 30/40 3K\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BG\"",\""Count\"":1,\""ProductTotalValue\"":2.16,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5600730945319\"",\""ProductName\"":\""BIMI I 800G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BG\"",\""Count\"":1,\""ProductTotalValue\"":6.78,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660999441\"",\""ProductName\"":\""MC COG PORTOBELLO I > 80MM 500G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""SW\"",\""Count\"":1,\""ProductTotalValue\"":2.98,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660955195\"",\""ProductName\"":\""MC SALADA IBERICA 4G 350G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.98,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4008100141568\"",\""ProductName\"":\""PEPINO VIN C / COENT 670GR HENG\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":3.99,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660996921\"",\""ProductName\"":\""RB ACUCAR G. STICKS 250X4G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.59,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660983051\"",\""ProductName\"":\""MC VINAGRE VB 5LT\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BX\"",\""Count\"":1,\""ProductTotalValue\"":2.55,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8436007955371\"",\""ProductName\"":\""MEL FRSC 500GR DIAMIR\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":3.45,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5608394811131\"",\""ProductName\"":\""QUINOA TRICOLOR BIO 1K ORIGENS\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":9.39,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601254058165\"",\""ProductName\"":\""FLOCOS AVEIA INT 1KG CIMARROM\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":1.48,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8715035330801\"",\""ProductName\"":\""MOLHO TERIYAKI 975ML KIKKOMAN\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BT\"",\""Count\"":1,\""ProductTotalValue\"":14.16,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8715035520806\"",\""ProductName\"":\""MOLHO PONZ\u0172 1L KIKKOMAN\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BT\"",\""Count\"":1,\""ProductTotalValue\"":9.99,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601660982498\"",\""ProductName\"":\""MC FARINHA T55 1KG\"",\""Unit\"":\""unit\"",\""Quantity\"":6,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":3.66,\""TaxCategory\"":\""Reduced\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4337182137467\"",\""ProductName\"":\""MC TORTILHA CHIPS 750G MILHO\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":3.59,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""7613034675842\"",\""ProductName\"":\""CHOCOLATE LION MINI 198G\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.99,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5000112556773\"",\""ProductName\"":\""COCA COLA REGULAR LATA 28X33CL\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BX\"",\""Count\"":28,\""ProductTotalValue\"":16.24,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""180IEC\"",\""ProductName\"":\""BEB A\u00c7UCAR. INCL.PUV\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""\"",\""Count\"":1,\""ProductTotalValue\"":2.14,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""5601488007854\"",\""ProductName\"":\""SACOS LIXO SILVEX 25X110 LT\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""BG\"",\""Count\"":1,\""ProductTotalValue\"":4.76,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8430117930204\"",\""ProductName\"":\""LUVAS NITRIL 4GASA PRET M 100U\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":100,\""ProductTotalValue\"":3.79,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8430117930211\"",\""ProductName\"":\""LUVAS NITRIL 4GASA PRET L 1000\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":100,\""ProductTotalValue\"":3.79,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4004133006522\"",\""ProductName\"":\""PRATO TART INOX + 2 TAMPAS\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":15.5,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4337182075981\"",\""ProductName\"":\""MPRO SALADEIRAS EMP.6CM CJ6\"",\""Unit\"":\""unit\"",\""Quantity\"":2,\""Container\"":\""BX\"",\""Count\"":6,\""ProductTotalValue\"":6.4,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8414793476782\"",\""ProductName\"":\""GASTRO FUN ARDOSIA 22X14 CM\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.85,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8414793476829\"",\""ProductName\"":\""GASTRO FRESH ARDOSIA 20X20 CM\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.85,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4333465971971\"",\""ProductName\"":\""SIGMA BOLSAS A4 PK100\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":100,\""ProductTotalValue\"":3.99,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4337182178361\"",\""ProductName\"":\""SIGMA R TERM 57MMX25M 48G CJ10\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":10,\""ProductTotalValue\"":6.09,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8413623107438\"",\""ProductName\"":\""R TERM 80X80X12 BF FREE 8UN\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":8,\""ProductTotalValue\"":19.79,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4008496559749\"",\""ProductName\"":\""PILHA VARTA H.ENERGY LR03 AAA\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":2.49,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""4008496746460\"",\""ProductName\"":\""PILHA VARTA ELECT. CR2032 BLI2\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":2,\""ProductTotalValue\"":2.99,\""TaxCategory\"":\""Intermediate\""}},\n  {{\""Id\"":null,\""ProductCode\"":\""8431241067866\"",\""ProductName\"":\""MPRO AVENTAL COZ.SARJA 1650 BR\"",\""Unit\"":\""unit\"",\""Quantity\"":1,\""Container\"":\""PC\"",\""Count\"":1,\""ProductTotalValue\"":8.9,\""TaxCategory\"":\""Intermediate\""}}\n],\""Comments\"":\""Products extracted from both pages\u2019 MAKRO tables. Tax mapping: IVA code 2\u2192Normal (23%), 4\u2192Reduced (6%), 5\u2192Intermediate (13%), validated against provided tax-category summary. Discount block 'Leve Mais Pague Menos' with code 43483 and amount 0,60 was applied entirely to product 7394376616709 (DD 43483), reducing its Valor Total from 12,54 to 11,94. Sum of final product totals and mapped IVA is consistent with TotalAmount 312,85 and TotalIVA 41,88 within rounding tolerance. Units/packaging inferred heuristically from PACK, Unit/KG and description; base Unit is set to 'unit' except when clearly weight-based (e.g. KG).\""}}""
        }}
      ],
      ""role"": ""assistant""
    }}
  ],
  ""parallel_tool_calls"": true,
  ""previous_response_id"": null,
  ""prompt_cache_key"": null,
  ""prompt_cache_retention"": null,
  ""reasoning"": {{
    ""effort"": ""low"",
    ""summary"": null
  }},
  ""safety_identifier"": null,
  ""service_tier"": ""default"",
  ""store"": true,
  ""temperature"": 1.0,
  ""text"": {{
    ""format"": {{
      ""type"": ""json_schema"",
      ""description"": null,
      ""name"": ""Invoice"",
      ""schema"": {{
        ""type"": ""object"",
        ""additionalProperties"": false,
        ""properties"": {{
          ""Products"": {{
            ""type"": ""array"",
            ""items"": {{
              ""type"": ""object"",
              ""additionalProperties"": false,
              ""properties"": {{
                ""Id"": {{
                  ""type"": [
                    ""integer"",
                    ""null""
                  ],
                  ""description"": ""ID of the product""
                }},
                ""ProductCode"": {{
                  ""type"": ""string"",
                  ""description"": ""Code of the product from the recognised invoice (if specified)""
                }},
                ""ProductName"": {{
                  ""type"": ""string"",
                  ""description"": ""Name of the product from the recognised invoice""
                }},
                ""Unit"": {{
                  ""type"": ""string"",
                  ""description"": ""Main unit of measurement of product (`kg`, `l`, `pcs`, `unit` etc.)""
                }},
                ""Quantity"": {{
                  ""type"": ""number"",
                  ""description"": ""Number of units or containers sold""
                }},
                ""Container"": {{
                  ""type"": ""string"",
                  ""description"": ""Packaging name/description (e.g., `Box6kg`, `24x0.33L`, `Pack250g`, `Btl 0.75l`)""
                }},
                ""Count"": {{
                  ""type"": ""number"",
                  ""description"": ""Quantity/volume/units per container (in the specified `Unit`, or in KG for weighed products and in L for liquids)""
                }},
                ""ProductTotalValue"": {{
                  ""type"": ""number"",
                  ""description"": ""Total cost of the product""
                }},
                ""TaxCategory"": {{
                  ""type"": ""string"",
                  ""enum"": [
                    ""Normal"",
                    ""Intermediate"",
                    ""Reduced"",
                    ""Zero""
                  ],
                  ""description"": ""Tax category of the product""
                }}
              }},
              ""required"": [
                ""Id"",
                ""ProductCode"",
                ""ProductName"",
                ""Unit"",
                ""Quantity"",
                ""Container"",
                ""Count"",
                ""ProductTotalValue"",
                ""TaxCategory""
              ]
            }}
          }},
          ""Comments"": {{
            ""type"": ""string"",
            ""description"": ""Additional comments""
          }}
        }},
        ""required"": [
          ""Products"",
          ""Comments""
        ]
      }},
      ""strict"": true
    }},
    ""verbosity"": ""medium""
  }},
  ""tool_choice"": ""none"",
  ""tools"": [],
  ""top_logprobs"": 0,
  ""top_p"": 1.0,
  ""truncation"": ""disabled"",
  ""usage"": {{
    ""input_tokens"": 54782,
    ""input_tokens_details"": {{
      ""cached_tokens"": 0
    }},
    ""output_tokens"": 4653,
    ""output_tokens_details"": {{
      ""reasoning_tokens"": 2102
    }},
    ""total_tokens"": 59435
  }},
  ""user"": null,
  ""metadata"": {{}}
}}";
    }
}
