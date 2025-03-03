
using Google.Protobuf;
using System.Text.Json;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;



namespace UpRestEye3.Services.Recognition
{
    public class GPTEnvironment2
    {
        private readonly string _invoiceAndRmsProductsSchema2;
        // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
        // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTEnvironment2()
        {
            _invoiceAndRmsProductsSchema2 = JsonHelper.GetMappedProductSchema();
        }



        public string GetInvoiceAndRmsProductsSchema2() => _invoiceAndRmsProductsSchema2;

        public string GetURL() => _url;

        public string GetApiKey() => _apiKey;



        public string GetReceiptMappingRequestBody2(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts, List<RMSMeasureUnitDTO> _measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();


            var _invoiceProducts = new List<InvoiceProductMappingDTO>(currentInvoice.Products.Select(p => new InvoiceProductMappingDTO()
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Unit = p.Unit
            }));
            var _rmsProducts = new List<RMSProductMappingDTO>(rmsProducts.Select(p => new RMSProductMappingDTO()
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Num = p.Num,
                Unit = p.MainUnit.ToString(), // todo убрать GUID, заменить на реальный юнит
                Containers = new List<RMSContainerMappingDTO>(p.Containers.Select(c => new RMSContainerMappingDTO()
                {
                    Id = c.Id,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count
                }))
            }));


            // Формируем запрос

            var requestData = new
            {
                model = "gpt-4o", //"gpt-4o-mini",
                temperature = 0.3,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = _systemMessage
                    },
                    new
                    {
                        role = "user",
                        content = @$"Match the following invoice products with the most appropriate RMS products and packaging from the restaurant system. Use predefined rules and return the results in the predefined JSON in the response_format section."
                    },
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new { InvoiceProducts = _invoiceProducts, RMSProducts = _rmsProducts, MeasureUnits = _measUnits }, options)

                    }
                },

                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "MatchedInvoiceProducts",
                        schema = JsonDocument.Parse(GetInvoiceAndRmsProductsSchema2()).RootElement
                    }
                }
            };
            return JsonSerializer.Serialize(requestData, options);
        }





        // todo подавать на вход все, что распознали в QR - суммы и налоговые категории. в промпте указать, чтобы сверил с тем что распознает
        // todo разобрать пример промпта в телеге, взять ключевые моменты оттуда


        private const string _systemMessage = @$"
You are an AI assistant that matches OCR-extracted invoice products with products from the restaurant management system. 
Your primary task is to ensure accurate mapping of `InvoiceProducts` to `RMSProducts`, considering product names, units, and packaging, and put mapped products into the predefined JSON in the response_format section. 
If a product is missing in the RMS system, create a new one following the structured guidelines.

**Matching Rules**:
- Take each product from the `InvoiceProducts` list and **copy all fields** of Invoice Product to the predefined JSON in the response_format section.
- Match the InvoiceProducts with the most appropriate RMSProducts and packaging from the restaurant system.
- Return a JSON object that strictly follows the response_format section. Ensure all fields are correctly formatted and do not omit any required fields.
- Product names may be abbreviated or translated (Portuguese ↔ English).
- Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
    - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name **but differ by color/type**, so they are **different products**. 
    - This should be taken into account for any different brands of wine or types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
    - Identify the main properties of both products - red or white, salty or sweet, frozen or fresh, and so on. And based on these properties, make your decision. 
    - Do not match products with different properties, even if they belong to the same brand.
- If a RMSProduct exists, **copy all fields** to the predefined JSON in the response_format section.
- If a RMSProduct exists but packaging does not, create a new packaging option as RMSContainer. Fill only the **Name and Count** (that means quantity, volume). Do not fill the **Id and Num fields**.
- If no matching RMSProduct exists, create a new one with a clear name. Fill only the **Name, Description and Unit**. Do not fill the **Id and Num fields**.
- When creating a new RMSProduct, also find the most appropriate Measure Unit for the new product in the **MeasureUnits dictionary** from the input data. Be careful, for weight products use kilograms (kg), for liquid products use litres (l), for piece products use pieces (pcs). Put the GUID of the selected Measure Unit into the mainUnit field of the new RMSProduct. 
- In the end, the measure units of the RMSProduct should logically match RMSContainers and the units of measure in InvoiceProduct. Check for compatibility again, if something does not match, then create a new RMSContainer or a new RMSProduct.
- **Minimize incorrect mappings** – when in doubt, prefer creating a new product.

Example Matching:
- InvoiceProduct: ""MORGADO QUINTAO BRANCO 2023 75CL 12%"" 
- RMSProduct: ""MORGADO QUINTAO BRANCO"" 
- Packaging match: 75CL -> Btl 0,75cl (use existing).";


        private const string _instruction2 = $@"
Be careful and use the following information:

01. Take each product from first to last from the InvoiceProducts list and find a suitable matching product in the RMSProducts List: 
    - Note that there may be abbreviated or branded product names in the Invoice Products list. And in the RMSProducts list there could be common names of products used to prepare dishes served in a restaurant. 
    - Product names in the Invoice Product List and in the RMSProducts list can be in Portuguese or English. If needed, interpret the names and make sure to consider possible translations (e.g., ""Tomate"" in Portuguese vs. ""Tomato"" in English).
    - Pay special attention to brand names, color, and product properties, especially for drinks such as wine:
            - For example, red (""tinto"" in Portuguese) and white (""branco"" in Portuguese) wines from the same winery may share most of the name **but differ by color/type**, so they are **different products**. **Do not match** them to the same RMS product if **one is red and the other is white**.
            - This should be taken into account for any different brands of wine or types of wine (sparkling, orange, rosé, etc.), as well as for other types of drinks and goods, for example ""Coca-Cola"" and ""Coca-Cola Zero"".
            - So, from the attributes in the name of the Invoice Product and RMSProduct, taking into account the possible different languages, English and Portuguese, identify the main properties of both products - red or white, salty or sweet, frozen or fresh, and so on. And based on these properties, make your decision. Do not match products with different properties, even if they belong to the same brand.
    - Use your knowledge of languages, typical foods and products used in restaurants to make the best match.
    - Note that the correct strategy is to minimise incorrect mappings. If there is the slightest doubt that the selected RMSProduct is suitable, continue searching. And if you don't find any that clearly fit, then feel free to create a new RMSProduct. 
    - Also try to find the most appropriate Container or Unit of measure for the Invoice Product being processed in the RMSContainers list of the selected RMSProduct. And if you can't find the right one, create a new RMSContainer.

02. Depending on the search results, select only one way: go to **02.01** if a matching **RMSProduct and RMSContainer are found**, go to **02.02** if **RMSProduct is not found**, go to **02.03** if a matching **RMSProduct is found** but **RMSContainer is not found**. Select only one option that matches the search results and perform only that option, do not perform the others:

    **02.01**. Only execute the current item if you **have found** the most appropriate RMSProduct and RMSContainer:
        - Before finalizing, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features. If you have any doubts, skip back to step 01.
        - Save the matched InvoiceProduct, RMSProduct and RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""false"" into the NewRMSProduct element.
        - Put ""false"" into the NewRMSContainer element.
        - Put description of the selection and potential issues in the Comments element.

    **02.02**. Only execute the current item if you **have not found** a matching RMSProduct:
        - Create a new RMSProduct based on the Invoice Product. Fill only the Name, Description and Unit. Do not fill the Id and Num fields.
        - Choose a common Name for the new RMSProduct without brand or abbreviation, if it is a typical grocery product to prepare dishes. If it is a product with an important brand name or a familiar product name (for example, some wine, or Coca-Cola), save this brand name in the RMSProduct.
        - Also create a new suitable RMSContainer. Fill only the Name and Count (that means quantity, volume). Do not fill the Id and Num fields.
        - Save the InvoiceProduct being processed, a new RMSProduct and a new RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""true"" into the NewRMSProduct element.
        - Put ""true"" into the NewRMSContainer element.
        - Put description of the creation and potential issues in the Comments element.

    **02.03**. Only execute the current item if you **have found** a matching RMSProduct, but **no matching** RMSContainer:
        - Before finalizing, double-check that you have found the most suitable product in the RMSProducts list, taking into account brand, color, language differences, and other specific product features. If you have any doubts, skip back to step 01.
        - Create a new suitable RMSContainer. Fill only the Name and Count (that means quantity, volume). Do not fill the Id and Num fields.
        - Save the matched InvoiceProduct, RMSProduct and a new RMSContainer as a new element of the response MatchedInvoiceProducts list.
        - Put ""false"" into the NewRMSProduct element.
        - Put ""true"" into the NewRMSContainer element.
        - Put description of the selection and creation and potential issues in the Comments element.

03. Check everything again. Make sure you make the best choice in the selected or created RMSProducts and RMSContainers, each choice is made according to the rules in the items **01** and **02**.

";

    }
}
