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
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Schema;

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
            string url = "https://api.openai.com/v1/chat/completions";
            string consumerNIF = "515409723";
            string consumerName = "Figueiredo";

            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-4o-mini", // "o1 -preview-2024-09-12",
                messages = new object[]
                {
                    new { role = "system", content = _system_prompt },//, consumerNIF},
                    new { role = "user", content = $@"Extract structured data from this receipt: {_request_prompt}" } //System.Text.Json.JsonSerializer.Serialize(invoiceText)}"}
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(_response_format).RootElement
                    }
                },
                temperature = 0.1
            };

            // Сериализация тела запроса
            var jsonBody = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            /*

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

            */
            // Чтение и возврат результата
            var responseContent = trainValue; //await response.Content.ReadAsStringAsync();

            
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

                    if(rootContent.TryGetProperty("title", out JsonElement titleElement) &&
                        titleElement.GetString() == "Invoice")
                    {
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateTimeJsonConverter(), new DecimalJsonConverter(), new IntegerJsonConverter()},
                            PropertyNameCaseInsensitive = true
                        };
                        using var invoiceDocument = JsonDocument.Parse(rootContent.GetProperty("properties").GetRawText());

                        
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
            

        private const string _system_prompt = $@"
You are a helpful assistant that structures OCR data into JSON.
Extract structured data from given receipts. 
1. Discard the unimportant information, leaving only the important data. 
2. Identify the seller (supplier), put his name and tax number and, if an IBAN bank account is found, in JSON in the Supplier section. 
3. Define the buyer, sometimes it is only a TAX-ID (NIF) and it should match this NIF: 515409723. Put the NIF and the name of the buyer in JSON in the Consumer section.
4. Determine the invoice number and date, put them in JSON in the Info section.
5. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
6. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
7. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
8. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
9. Add your comments about recognized data in the Comments section.
";

        /*
         * 
         *         private const string _system_prompt = $@"
        You are a helpful assistant that structures OCR data into JSON.
        Extract structured data from given receipts. 
        1. Discard the unimportant characters and unnecessary information, leaving only the important data. 
        2. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
        3. Identify the seller (supplier), put his name and tax number and, if an IBAN bank account is found, in JSON in the Supplier section. 
        4. Define the buyer, sometimes it is only a TAX-ID (NIF) and it should match this NIF:{{consumerNIF}} or Name should be like ""{{consumerName}}"". Put the NIF and the name of the buyer in JSON in the Consumer section.
        5. Determine the invoice number and date, put them in JSON in the Info section.
        6. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
        7. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
        6. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
        7. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
        ";

         * 
         * 
         */

        private const string _response_format = $@"
{{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""title"": ""Invoice"",
  ""type"": ""object"",
  ""properties"": {{
    ""Supplier"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""Name"": {{
          ""type"": ""string""
        }},
        ""TaxNumber"": {{
          ""type"": ""string""
        }},
        ""BankAccount"": {{
          ""type"": [""string"", ""null""]
        }}
      }}
    }},
    ""Consumer"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""Name"": {{
          ""type"": ""string""
        }},
        ""TaxNumber"": {{
          ""type"": ""string""
        }}
      }}
    }},
    ""Info"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""InvoiceNumber"": {{
          ""type"": ""string""
        }},
        ""InvoiceDate"": {{
          ""type"": ""string"",
          ""format"": ""date-time""
        }},
        ""TotalAmountInclTaxes"": {{
          ""type"": ""number""
        }},
        ""TotalAmountExclTaxes"": {{
          ""type"": ""number""
        }}
      }}
    }},
    ""Products"": {{
      ""type"": ""array"",
      ""items"": {{
        ""type"": ""object"",
        ""properties"": {{
          ""ProductCode"": {{
            ""type"": ""string""
          }},
          ""ProductName"": {{
            ""type"": ""string""
          }},
          ""Unit"": {{
            ""type"": ""string""
          }},
          ""Quantity"": {{
            ""type"": ""number""
          }},
          ""Price"": {{
            ""type"": ""number""
          }}
        }}
      }}
    }},
    ""TaxCategories"": {{
      ""type"": ""array"",
      ""items"": {{
        ""type"": ""object"",
        ""properties"": {{
          ""Category"": {{
            ""type"": ""string""
          }},
          ""Amount"": {{
            ""type"": ""number""
          }}
        }}
      }}
    }},
    ""FilePath"": {{
      ""type"": [""string"", ""null""]
    }},
    ""UploadTime"": {{
      ""type"": ""string"",
      ""format"": ""date-time""
    }},
    ""Comments"": {{
      ""type"": ""string""
    }},
    ""Status"": {{
      ""type"": ""string""
    }}
  }}
}}
";

private const string _request_prompt = $@"
{{
  ""TextBlocks"": [
    
    {{
      ""BlockNumber"": 1,
      ""Paragraphs"": [
        {{
          ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Superlativo Oasis""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Contribuinte: 516398946 ""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""Rua Paulo da Gama 629 4150-589 Porto Portugal""
        }}
      ]
    }},
    {{
    ""BlockNumber"": 12,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Figueiredo \u0026 Andrade, Unipessoal Limitada""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Contribuinte: 515409723 ""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""Avenida 1 de Maio, N36G, 2825-393 Costa da Caparica, Portugal""
        }}
      ]
    }},

    {{
    ""BlockNumber"": 18,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Fatura FT 2024/123""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Data Vencimento 14-10-2024""
        }},
      ]
    }},
    
    {{
    ""BlockNumber"": 21,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Codigo""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Descricao""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""Qtd.""
        }},
        {{
        ""ParagraphNumber"": 3,
          ""ParagraphText"": ""Preco""
        }},
        {{
        ""ParagraphNumber"": 4,
          ""ParagraphText"": ""Total""
        }},
        {{
        ""ParagraphNumber"": 5,
          ""ParagraphText"": ""IVA""
        }}
      ]
    }},

    {{
    ""BlockNumber"": 22,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""00112233""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Boina Tinto""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""12""
        }},
        {{
        ""ParagraphNumber"": 3,
          ""ParagraphText"": ""6""
        }},
        {{
        ""ParagraphNumber"": 4,
          ""ParagraphText"": ""72""
        }},
        {{
        ""ParagraphNumber"": 5,
          ""ParagraphText"": ""13%""
        }}
      ]
    }},

    {{
    ""BlockNumber"": 23,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""TAXA""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""IVA""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""INCID""
        }}
      ]
    }},
    {{
    ""BlockNumber"": 24,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""13%""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""9.36""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""72""
        }}
      ]
    }},
    
    {{
    ""BlockNumber"": 27,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Subtotal""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""Total IVA""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""Total""
        }}
      ]
    }},
    {{
    ""BlockNumber"": 28,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""72""
        }},
        {{
        ""ParagraphNumber"": 1,
          ""ParagraphText"": ""9.36""
        }},
        {{
        ""ParagraphNumber"": 2,
          ""ParagraphText"": ""81.36""
        }}
      ]
    }},
    
    {{
    ""BlockNumber"": 45,
      ""Paragraphs"": [
        {{
        ""ParagraphNumber"": 0,
          ""ParagraphText"": ""Millennium BCP : PT50 0033 0000 45631842115 05 ""
        }}
      ]
    }}
  ]
}}";

string trainValue = @$"{{
  ""id"": ""chatcmpl-AgF63XFsbDhm00hZDU36Rgbnodeqo"",
  ""object"": ""chat.completion"",
  ""created"": 1734631171,
  ""model"": ""gpt-4o-mini-2024-07-18"",
  ""choices"": [
    {{
      ""index"": 0,
      ""message"": {{
        ""role"": ""assistant"",
        ""content"": ""{{\n  \""$schema\"": \""http://json-schema.org/draft-07/schema#\"",\n  \""title\"": \""Invoice\"",\n  \""type\"": \""object\"",\n  \""properties\"": {{\n    \""Supplier\"": {{\n      \""Name\"": \""Pupermotivo\"",\n      \""TaxNumber\"": \""518390947\"",\n      \""BankAccount\"": \""PT50 0033 0000 45631834432 08\""\n    }},\n    \""Consumer\"": {{\n      \""Name\"": \""Figueiredo & Andrade, Unipessoal Limitada\"",\n      \""TaxNumber\"": \""515409723\""\n    }},\n    \""Info\"": {{\n      \""InvoiceNumber\"": \""FT 2024/123\"",\n      \""InvoiceDate\"": \""14-10-2024\"",\n      \""TotalAmountInclTaxes\"": 81.36,\n      \""TotalAmountExclTaxes\"": 72\n    }},\n    \""Products\"": [\n      {{\n        \""ProductCode\"": \""00112233\"",\n        \""ProductName\"": \""Boina Tinto\"",\n        \""Unit\"": \""piece\"",\n        \""Quantity\"": 12,\n        \""Price\"": 6\n      }}\n    ],\n    \""TaxCategories\"": [\n      {{\n        \""Category\"": \""13%\"",\n        \""Amount\"": 9.36\n      }}\n    ],\n    \""FilePath\"": null,\n    \""UploadTime\"": \""20.12.2024\"",\n    \""Comments\"": \""The total amount with tax matches the sum of the items listed.\"",\n    \""Status\"": null\n  }}\n}}"",
        ""refusal"": null
      }},
      ""logprobs"": null,
      ""finish_reason"": ""stop""
    }}
  ],
  ""usage"": {{
    ""prompt_tokens"": 1540,
    ""completion_tokens"": 322,
    ""total_tokens"": 1862,
    ""prompt_tokens_details"": {{
      ""cached_tokens"": 0,
      ""audio_tokens"": 0
    }},
    ""completion_tokens_details"": {{
      ""reasoning_tokens"": 0,
      ""audio_tokens"": 0,
      ""accepted_prediction_tokens"": 0,
      ""rejected_prediction_tokens"": 0
    }}
  }},
  ""system_fingerprint"": ""fp_0aa8d3e20b""
}}
";


}

}
