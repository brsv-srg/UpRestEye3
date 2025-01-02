using System.Collections.Generic;
using System.Text.Json;
using UpRestEye3.Models;


namespace UpRestEye3.Services
{
    public class GPTEnvironment 
    {
        private readonly string _consumerNIF;
        private readonly string _consumerName;
        private readonly string _systemPrompt;
        private readonly string _responseFormat;
        private readonly string _testRequestPrompt;
        private readonly string _testResponse;

        public GPTEnvironment(string ConsumerName, string ConsumerNIF) 
        {
            _consumerName = ConsumerName;
            _consumerNIF = ConsumerNIF;
            //_systemPrompt = String.Format(_systemPromptLiteral, _consumerNIF, _consumerName);
            _systemPrompt = String.Format(_testSystemPromptLiteral, _consumerNIF, _consumerName);
            _responseFormat = GetInvoiceJsonSchema(); // _responseFormatLiteral;
            _testRequestPrompt = _testRequestPromptLiteral;
            _testResponse = _trainResponseLiteral;


        }

        public string GetSystemPrompt()
        {
            return _systemPrompt;
        }

        public string GetResponseFormat()
        {
            return _responseFormat;
        }

        public string GetTestRequestPrompt()
        {
            return _testRequestPrompt;
        }

        public string GetTestResponse()
        {
            return _testResponse;
        }
        
        public string GetInvoiceJsonSchema()
        {
            var invoiceType = typeof(Invoice);
            var schema = GenerateJsonSchema(invoiceType);
            return schema;
        }

        private string GenerateJsonSchema(Type type)
        {
            var properties = type.GetProperties();
            var schema = new
            {
                schema = "http://json-schema.org/draft-07/schema#",
                title = type.Name,
                type = "object",
                properties = properties.ToDictionary(
                    prop => prop.Name,
                    prop => new
                    {
                        type = GetJsonType(prop.PropertyType)
                    })
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
        }

        private string GetJsonType(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return "array";
            if (type.IsClass)
                return "object";

            return "string";
        }


        private const string _systemPromptLiteral = $@"
You are a helpful assistant that structures OCR data into JSON.
Extract structured data from given receipts. 
01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. Each text block is accompanied by the coordinates of its location on the receipt. Determine the relationship between the data based on these coordinates. 
04. Define the name and TAX-ID of the vendor (or supplier) in the document, they are placed next to each other. The TAX-ID of the supplier should not be equal to {{0}}, and the name should not be similar to ""{{1}}"". Also try to find the supplier bank account - IBAN. It could be located next to the supplier's name or at the end of the document, but it may not be there in the document. Take the TAX-ID, name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
05. Try to define the consumer, sometimes it is only a TAX-ID (NIF) and it should match this NIF:{{0}}. Sometimes there can also be a Name and you should try to identify it. It's located next to the TAX-ID and may contain or be similar to ""{{1}}"". You need to take the consumer Name from the processed document  and the consumer's consumer TAX-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
06. Determine the invoice number and date, put them in JSON in the Info section.
07. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
08. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
09. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
10. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
11. Add your comments about recognized data in the Comments element of the response JSON.
";

        private const string _testSystemPromptLiteral = $@"
You are a helpful assistant that structures OCR data into JSON.
Extract structured data from given receipts. 
01. Discard the unimportant characters and unnecessary information, leaving only the important data. 
02. The response must strictly adhere to the JSON structure provided in the response_format. No additional data such as schema descriptions or extra data.
03. Define the name and TAX-ID of the vendor (or supplier) in the document, they are placed next to each other. The TAX-ID of the supplier should not be equal to {{0}}, and the name should not be similar to ""{{1}}"". Also try to find the supplier bank account - IBAN. It could be located next to the supplier's name or at the end of the document, but it may not be there in the document. Take the TAX-ID, name and IBAN from the processed document and put them into the Supplier section of the JSON response. 
04. Try to define the consumer, sometimes it is only a TAX-ID (NIF) and it should match this NIF:{{0}}. Sometimes there can also be a Name and you should try to identify it. It's located next to the TAX-ID and may contain or be similar to ""{{1}}"". You need to take the consumer Name from the processed document  and the consumer's consumer TAX-ID and put them into the Consumer section of the JSON response. Sometimes the document may not contain the consumer data, but if it does, it must fulfil the conditions specified in this paragraph, you need to check everything again. 
05. Determine the invoice number and date, put them in JSON in the Info section.
06. Define the list of items: their names, quantity (piece or by weight) and cost. As a rule, the list in the receipt has a tabular form. Determine this on the basis of the coordinates. Put the list in JSON in the Products section.
07. Determine the tax amounts by category, if present on the receipt, put a list of them in JSON in the TaxCategories section.
08. Determine the total amount with tax and the total amount without tax, put them in JSON in the Info section. 
09. Verify that the sum of the items in the Products list is equal to the sum in the Total section.
10. Add your comments about recognized data in the Comments element of the response JSON.
";


        private const string _responseFormatLiteral = $@"
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
        ""TotalTax"": {{
          ""type"": ""number""
        }},
        ""TotalAmount"": {{
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

        private const string _testRequestPromptLiteral = $@"
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

        private const string _trainResponseLiteral = @$"{{
  ""id"": ""chatcmpl-AgF63XFsbDhm00hZDU36Rgbnodeqo"",
  ""object"": ""chat.completion"",
  ""created"": 1734631171,
  ""model"": ""gpt-4o-mini-2024-07-18"",
  ""choices"": [
    {{
      ""index"": 0,
      ""message"": {{
        ""role"": ""assistant"",
        ""content"": ""{{\n  \""$schema\"": \""http://json-schema.org/draft-07/schema#\"",\n  \""title\"": \""Invoice\"",\n  \""type\"": \""object\"",\n  \""properties\"": {{\n    \""Supplier\"": {{\n      \""Name\"": \""Pupermotivo\"",\n      \""TaxNumber\"": \""518390947\"",\n      \""BankAccount\"": \""PT50 0033 0000 45631834432 08\""\n    }},\n    \""Consumer\"": {{\n      \""Name\"": \""Figueiredo & Andrade, Unipessoal Limitada\"",\n      \""TaxNumber\"": \""515409723\""\n    }},\n    \""Info\"": {{\n      \""InvoiceNumber\"": \""FT 2024/123\"",\n      \""InvoiceDate\"": \""14-10-2024\"",\n      \""TotalTax\"": 81.36,\n      \""TotalAmount\"": 72\n    }},\n    \""Products\"": [\n      {{\n        \""ProductCode\"": \""00112233\"",\n        \""ProductName\"": \""Boina Tinto\"",\n        \""Unit\"": \""piece\"",\n        \""Quantity\"": 12,\n        \""Price\"": 6\n      }}\n    ],\n    \""TaxCategories\"": [\n      {{\n        \""Category\"": \""13%\"",\n        \""Amount\"": 9.36\n      }}\n    ],\n    \""FilePath\"": null,\n    \""UploadTime\"": \""20.12.2024\"",\n    \""Comments\"": \""The total amount with tax matches the sum of the items listed.\"",\n    \""Status\"": null\n  }}\n}}"",
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
