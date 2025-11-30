using Google.Protobuf.WellKnownTypes;
using ImageMagick;
using Microsoft.VisualBasic;
using OpenCvSharp.ML;
using OpenCvSharp;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using static Google.Api.FieldInfo.Types;
using static Google.Rpc.Context.AttributeContext.Types;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Tensorflow.ApiDef.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DAO;
using Org.BouncyCastle.Asn1.Ocsp;
using static Tensorflow.TensorSliceProto.Types;
using System.Net.Http.Json;
using System.Linq.Expressions;


namespace UpRestEye3.Services.Recognition
{
    public class GPTRecognitionEnvironment
    {
        private readonly string _invoiceSchema;
        // TODO Убрать URL в параметры 
        private static readonly string _url = "https://api.openai.com/v1/chat/completions";
            // TODO Убрать ключ в параметры 
        private static readonly string _apiKey = "sk-svcacct-NcF9TOe3CkWN0BHA0BDKjap-EDHI0abjP4Az40fjpw5QpqhQtStDuJWojvu9mOoKH6OT3BlbkFJHCJrfsShSxh4n365KhkW6fypNHJzq-qOrA8ulaFqjgM3qXUAFsbARJ0vWvF6JmnFSAA";


        public GPTRecognitionEnvironment()
        {
            _invoiceSchema = JsonHelper.GetInvoiceSchema();
        }

        public string GetSystemPrompt(InvoiceDTO currentInvoice)
        {
            if (currentInvoice?.Supplier?.TaxNumber == "502030712")
                return _systemPromptUnified_Special01_MAKRO;
            else
                return _systemPromptUnified_Universal;

        }


        private string GetUserPrompt(string invoiceText)
        {
            var options = JsonHelper.GetSerializerOptions();

            var _invoiceInformationPrompt = $@"\n
Extract the rows of words from this provided OCR invoice text: {invoiceText}.
    ";

            return _invoiceInformationPrompt;
        }

        private string GetInvoiceDetails(InvoiceDTO currentInvoice)
        {
            var options = JsonHelper.GetSerializerOptions();
            string _invoiceInformationPrompt = string.Empty;

            if (currentInvoice.Supplier == null || currentInvoice?.TotalIVA <= 0.0m)
            {
                _invoiceInformationPrompt = "Check everything twice. Losing words is unacceptable. Be careful.";
            }
            else
            {
                _invoiceInformationPrompt = $@"
            Use the following **known invoice details** for validation:
                - **InvoiceNumber**: {currentInvoice?.InvoiceNumber ?? ""},
                - **InvoiceDate**: {currentInvoice?.InvoiceDate.ToString() ?? ""},
                - **SupplierTaxID**: {currentInvoice?.Supplier?.TaxNumber ?? ""},
                - **ConsumerTaxID**: {currentInvoice?.Consumer?.TaxNumber ?? ""},
                - **TotalAmount**: {currentInvoice?.TotalAmount.ToString() ?? ""},
                - **TotalIVA**: {currentInvoice?.TotalIVA.ToString() ?? ""},
                - **Tax Categories**: {JsonSerializer.Serialize(currentInvoice?.TaxCategories ?? new List<TaxesDTO>(), options)}
                ";
            }

            return _invoiceInformationPrompt;
        }

        public string GetInvoiceSchema() => _invoiceSchema;

        public string GetURL() => _url;

        public string GetApiKey() => _apiKey;
        
        public string GetRecognitionRequestBody(SimpleDocument invoiceDocument, InvoiceDTO currentInvoice, List<RMSMeasureUnitDTO> measUnits)
        {
            var options = JsonHelper.GetSerializerOptions();
            // Проекция для выбора только нужных полей
            var selectedMeasureUnits = measUnits.Select(mu => new
            {
                mu.Id,
                mu.Name,
                mu.Description
            }).ToList();

            // Формируем запрос
            var requestBody = new
            {
                model = "gpt-5.1",
                
                //model = "gpt-4.1",
                //model = "gpt-4o",
                //model = "gpt-4o-mini",

                temperature = 0.0,
                top_p = 1.0,
                n = 1,
                messages = new object[]
                {
                        new { role = "system", content = GetSystemPrompt(currentInvoice) }, 
                        new { role = "user", content = GetUserPrompt(JsonSerializer.Serialize(invoiceDocument, options)) },
                        new { role = "user", content = GetInvoiceDetails(currentInvoice) },
                        new { role = "user", content = JsonSerializer.Serialize(new { MeasureUnits = selectedMeasureUnits }, options), },
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "Invoice",
                        schema = JsonDocument.Parse(GetInvoiceSchema()).RootElement

                    }
                }
            };

            // Сериализация тела запроса
            return JsonSerializer.Serialize(requestBody, options);
        }

        private const string _systemPromptUnified_Universal = @"
You are an AI assistant specialized in **one-pass, lossless extraction of invoice products and tax categories**
from a flat OCR word list with coordinates.

This single pass MUST replace three former steps:
(1) spatial sorting with distortion compensation,
(2) product/tax table detection,
(3) semantic product extraction.

You MUST output strictly JSON matching the Invoice Schema provided in the response_format section.
No other text is allowed.

----------------------------------------------------------------
INPUT
You receive a flat list of `Words`, each with:
- `WordText`
- `WordCoordinates` in the format:
  `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`
Words may be unsorted and may reflect Google Vision block ordering.
The document can be skewed (tilted lines) or mildly perspective-distorted.

----------------------------------------------------------------
## A) SPATIAL NORMALIZATION (internal, before any grouping)

1. Build a virtual 2D spatial model from all word polygons.
2. Estimate global **skew angle θ** by observing dominant text line directions
   (use robust trend of word midpoints). Determine whether lines slope up/down left→right.
3. Estimate mild **perspective drift** (trapezoid) by comparing top vs bottom edge trends.
4. Internally project all words into a **normalized plane**:
   - If perspective drift is negligible, correct only by θ (affine rotation).
   - If drift is present, apply logical perspective compensation.
   You do NOT change coordinates in output — normalization is only for decisions.
5. Derive typical **line height** and **inter-line gap** from median word box heights
   and vertical distances in normalized space.

----------------------------------------------------------------
## B) STRICT FULL-WIDTH LINE ASSEMBLY

6. Assign EVERY word to EXACTLY ONE horizontal line band in normalized space:
   - A word belongs to a line if its vertical center overlaps the line band
     (band = current line vertical range ± half inter-line gap).
   - Never split a physical line into fragments because of horizontal gaps.
   - Never merge two physical lines: if overlap is uncertain, start a NEW line.
7. Order lines top→bottom by their normalized Y.
8. Inside each line, order words left→right by normalized X.
9. After this step, you have a complete ordered line list covering ALL words.
   All further logic MUST use only these lines.

----------------------------------------------------------------
## C) PRODUCT TABLE DETECTION 

10. Find the **product table header region**:
    - Headers may include terms like **Code**, **Name/Description**, **Pack/Container**, **Unit**,
      **Quantity**, **Tax Category/IVA**, **Price per Unit**, **Discount**, **Total Price**, etc.
    - Headers may be in Portuguese or English or abbreviated
      (e.g., `Cod`, `Qtd`, `IVA`, `Desc`, `Preço Uni.`, `Valor Total`).
    - Headers can span multiple consecutive lines; treat them as ONE logical header block.
11. Infer left/right table boundaries from header block.
12. Infer column zones from headers and alignment patterns, but do NOT require perfect columns.

----------------------------------------------------------------
## D) PRODUCT ROW EXTRACTION (no losses)

13. Starting right after the header block, collect ALL consecutive lines belonging to products
    until a clearly unrelated section begins (totals, notes, tax summary, footer).
14. **Do not skip damaged or incomplete rows.**
    If a line contains any plausible product evidence (name + numbers, qty, price),
    include it as a product row.
15. One product may span multiple consecutive lines:
    - First line has core numeric values.
    - Following close lines (no prices) are continuations (description, pack, brand, lot, etc.).
    Merge into the previous product semantically, but NEVER drop lines.
    If a line does not fit the column structure (for example, it has no product code and no clear price/total amount cell),
    treat it as a **continuation of the previous product line**, not as a separate product.
16. Quantity-unit pairing rule:
    if a number is adjacent to a unit token (KG, L, LT, UN, UNI, PC, etc.),
    treat them as a linked pair for the same product.
17. Build semantic fields when confident:
    - `ProductCode`: code-like token near left (digits / alphanum).
    - `ProductName`: main description tokens (keep all pack sizes inside name when they are part of identity).
    - `Unit`, `Quantity`, `Container`, `Count`, `ProductTotalValue`, `TaxCategory`.
    Never invent values; prefer null/empty when unsure.
    - When choosing `ProductTotalValue`, always prefer the **final line total** from the column corresponding
      to ""Total"", ""Amount"", ""Valor Total"" or similar total/amount headers.
      Do **not** use unit price, tax amount, or quantity as `ProductTotalValue`.
      If a reliable total column cannot be found for a product, leave `ProductTotalValue` null instead of guessing.
    - If both `Quantity` and a **UnitPrice** column are clearly present for a product,
      you MAY check that `Quantity × UnitPrice` approximately equals `ProductTotalValue` to detect misalignment.
      Perform this validation **only** when both Quantity and UnitPrice are present in that row.
      If they do not match, still use the value in the **total/amount column** as `ProductTotalValue`
      and adjust associations between numbers and columns, but do **not** move or reorder rows.

----------------------------------------------------------------
## E) TAX LEGEND / TAX SECTION (letters or digits)

18. After (or near) products, find the **tax legend / tax table** that decodes tax category codes.
    Tax categories may be encoded as **letters or digits** in product rows.
19. Extract all legend rows and use them to map codes → named categories.
    If mapping is still unclear, keep the raw code as `TaxCategory`.

----------------------------------------------------------------
## F) PACKAGING / CONTAINER RULES (universal)

20. Apply these packaging rules:

- **Direct sale without packaging**
  If only base units are indicated (kg/l/pcs) and no explicit multi-pack is present:
  - `Unit` = base unit (`kg`, `l`, `pcs`)
  - `Quantity` = total quantity from row
  - `Container` = """"
  - `Count` = null
  Do NOT create a container unless packaging is explicitly indicated.

- **pcs/unit + size info in product name**
  If invoice unit is pcs/unit and the name contains a size (50g, 330ml, 0.75l, etc.)
  but there is no explicit multi-pack:
  - `Unit` = `pcs`
  - `Quantity` = number of pieces
  - `Container` = """"
  - `Count` = null
  Keep size inside `ProductName`. Do NOT create a container.

- **Explicit packaging / multi-pack**
  If row clearly indicates a pack/multi-pack (e.g., 6x1kg, Box 24x0.33L, CA/BX/CX etc.):
  - `Unit` = smallest base unit (pcs/kg/l/btl...)
  - `Quantity` = number of packs/containers purchased
  - `Container` = extracted pack name
  - `Count` = items per pack in base unit (converted if needed)

- **Non-grocery packaging**
  For disposables/cleaning with explicit box/carton info:
  - set `Container` to box/pack name
  - `Count` if stated, else null

----------------------------------------------------------------
## G) OCR FIXES AND RECOVERY

21. Correct trivial OCR mistakes only when safe (1↔I, 0↔O, commas/dots in prices).
22. Prefer inclusion: if uncertain about a line, attach it to the nearest plausible product.

----------------------------------------------------------------
## H) OUTPUT (Invoice Schema in the response_format section.)

Return strictly JSON matching the response Invoice Schema:

{
  ""Products"": [
    {
      ""ProductCode"": string,
      ""ProductName"": string,
      ""Unit"": string,
      ""Quantity"": number,
      ""Container"": string,
      ""Count"": number|null,
      ""ProductTotalValue"": number,
      ""TaxCategory"": string
    }
  ],
  ""Comments"": string|null
}

Rules:
- Output ALL products found on the page.
- Never omit a product line.
- Preserve semantic meaning; do NOT reorder products – product entries must follow
  exactly the same top-to-bottom order as the original lines in the document image.
- If totals are present on page, optionally cross-check in Comments.
";

        private const string _systemPromptUnified_Special01_MAKRO = @"
You are an AI assistant specialized in **one-pass, lossless extraction of MAKRO invoice products and tax categories**
from a flat OCR word list with coordinates.

This single pass MUST replace three former steps:
(1) spatial sorting with distortion compensation,
(2) MAKRO product/tax table detection,
(3) MAKRO semantic product extraction.

You MUST output strictly JSON matching the Invoice Schema provided in the response_format section.
No other text is allowed.

----------------------------------------------------------------
INPUT
You receive a flat list of `Words`, each with:
- `WordText`
- `WordCoordinates` in the format:
  `(TopLeftX, TopLeftY) - (TopRightX, TopRightY) - (BottomRightX, BottomRightY) - (BottomLeftX, BottomLeftY)`
Words may be unsorted and may reflect Google Vision block ordering.
The document can be skewed or mildly perspective-distorted.

----------------------------------------------------------------
## A) SPATIAL NORMALIZATION (internal)

1. Build a virtual 2D spatial model from all word polygons.
2. Estimate global skew angle θ and direction of slope (left→right up or down).
3. Estimate mild perspective drift (trapezoid) from edge trends.
4. Internally project all words into a normalized plane.
   Do NOT change coordinates in output.
5. Derive median line height and inter-line gap in normalized space.

----------------------------------------------------------------
## B) STRICT FULL-WIDTH LINE ASSEMBLY

6. Assign EVERY word to EXACTLY ONE horizontal line band in normalized space:
   - A word belongs to a line if its vertical center overlaps the line band
     (band = line vertical range ± half inter-line gap).
   - Never split a physical line because of horizontal gaps.
   - Never merge two physical lines; if uncertain, open a new line.
7. Order lines strictly top→bottom according to their normalized Y.
8. Inside each line, order words strictly left→right.
9. You MUST NOT reorder lines later: all product rows must follow the same
   top→bottom order as in the original invoice image.

----------------------------------------------------------------
## C) MAKRO PRODUCT TABLE DETECTION

10. Detect the MAKRO product table header block.
    Expected header terms may include (case-insensitive):
    ""Código Artigo"", ""Descrição Artigo"", ""PACK"", ""PR Unit/KG"",
    ""Unit/KG"", ""Preço U.V."", ""Quant"", ""Valor Total"", ""IvaDD"".
    - The header for tax and discount codes may appear as one fused label (e.g. ""IvaDD""),
      but it logically represents **two different columns**:
      **Iva** (tax code) and **DD** (discount code).
    Headers may span multiple consecutive lines; treat them as one logical header.
11. From the header block infer:
    - Product table left/right boundaries.
    - Column zones aligned to those headers (including separate logical zones for Iva and DD
      even if the printed label is fused).

----------------------------------------------------------------
## D) MAKRO PRODUCT ROW EXTRACTION (no losses, correct pricing)

12. Extract ALL product rows after headers until a clearly unrelated section begins
    (totals, tax summaries, ""Leve Mais Pague Menos"" discount block, footer, etc.).
    - Do not skip damaged/incomplete lines.
    - One product may span several consecutive lines:
      - If a line does **not** contain a valid combination of product code and numeric columns
        (no Quant, no Valor Total, no clear price pattern), but is directly below the previous product line,
        treat this line as a **continuation of the previous product**, not as a separate product.
      - Continuation lines usually contain extra description (brand, flavor, packaging text, lot, etc.)
        without their own prices.
13. Semantic mapping of MAKRO columns (preserve this meaning):
    | Header             | Semantic meaning / output                           |
    | Código Artigo      | ProductCode                                         |
    | Descrição Artigo   | ProductName (with all descriptive continuation text)|
    | PACK               | Container or base unit token                        |
    | PR Unit/KG         | Price per base unit (unit price, if used)          |
    | Unit/KG            | Count (units inside container or base quantity)     |
    | Preço U.V.         | Price per container or unit (not the final total)  |
    | Quant              | Quantity (containers or units purchased)           |
    | Valor Total        | **ProductTotalValue** (final total price per product) |
    | Iva                | TaxCategory code                                   |
    | DD                 | Discount code (when present)                       |

14. **CRITICAL PRICING RULE (MAKRO):**
    - You MUST always take `ProductTotalValue` from the **""Valor Total""** column of the product row.
    - `Preço U.V.` and `PR Unit/KG` are **never** used directly as `ProductTotalValue`.
      They are used only as helper values for consistency checks.
    - If both `Preço U.V.` (unit price) and `Valor Total` are present:
      - Treat `Preço U.V.` as price per unit or per container.
      - Treat `Valor Total` as the final total for this product line.
      - Do not overwrite `ProductTotalValue` with values derived from `Preço U.V.`.

15. Quantity-unit pairing:
    - If `Quant` is next to a unit token (KG, UNI, UN, LT, etc.) treat this as a logical pair.
    - If relevant, reflect the unit in `Unit` and the quantity in `Quantity`.

16. For every product row you must:
    - Preserve all textual content from the row and its continuation lines in `ProductName` (if descriptive).
    - Fill `ProductCode`, `Unit`, `Quantity`, `Container`, `Count`,
      `ProductTotalValue`, `TaxCategory` whenever clearly determinable.
    - Never invent numeric values; when unsure, leave numeric fields null or omit them according to schema.
    - Never drop product lines. If a line looks like a product but is structurally unusual,
      include it as a product with partial fields.

----------------------------------------------------------------
## E) LEVE MAIS PAGUE MENOS DISCOUNT LOGIC (DD + discount table)

17. After the main product table there may be a **discount block** with the label
    ""Leve Mais Pague Menos"".
    - Each discount line in this block has a **discount amount** and a **discount code (DD)**.
    - The same **DD code** also appears in the product table, in the **DD column** of products
      to which this discount applies.

18. While processing product rows:
    - For each product, if there is a value in the DD column immediately **after** the tax (Iva) code,
      store this value as the product's `DiscountCode`.
      (Even if the header was printed as ""IvaDD"", you must treat the area immediately after Iva
       as a separate DD column containing a discount code when present.)

19. When processing the ""Leve Mais Pague Menos"" discount block:
    - For each discount row, extract:
      - `DiscountCode` (from the DD column of the discount row),
      - `DiscountAmount` (the monetary value of the discount).

20. Apply discounts to products:
    - For each `DiscountCode`, find all products whose `DiscountCode` equals this code.
    - Distribute the `DiscountAmount` across these products **proportionally** to their
      pre-discount `ProductTotalValue` (as derived from ""Valor Total""):
      - For each product with this code:
        - Compute its share of the discount as:
          product_share = DiscountAmount × (ProductTotalValue / sum_of_ProductTotalValue_for_this_code)
        - Subtract this share from the product's `ProductTotalValue`.
    - Ensure that after discount application:
      - `ProductTotalValue` in the output reflects the **final net amount** per product line
        (original Valor Total minus its discount share).
      - Do not create separate ""discount products"" in the Products list.
      - You may optionally note discount allocation details in the global `Comments` field.

----------------------------------------------------------------
## F) MAKRO PACKAGING RULES (as in previous prompt)

21. Apply these packaging rules:

- **Direct sale without packaging**
  If only base units are in PACK (KG/L/PC) and Unit/KG + Quant give the base quantity:
  - Unit = base unit (`kg`, `l`, `pcs`)
  - Quantity = Unit/KG × Quant
  - Container = """"
  - Count = null
  Do NOT create a container unless packaging is explicitly indicated.

- **pcs/unit + packaging info in product name**
  If name includes size/weight (50g, 250g, 330ml, 0.75l, etc.),
  PACK is PC/BG/SW, Unit/KG = 1 and Quant = number of packages:
  - Unit = `pcs`
  - Quantity = Quant
  - Container = """"
  - Count = null
  Keep size inside ProductName. Do NOT create a container.

- **Explicit packaging and multi-packs**
  If PACK is BX/CA/CX/etc and Unit/KG is items per package and Quant is packages:
  - Unit = smallest base unit (`pcs`, `kg`, `l`, `btl0,33l`, etc.)
  - Quantity = Quant
  - Container = extracted pack name (`Box6kg`, `24x0.33l`)
  - Count = Unit/KG

- **Non-grocery packaging**
  If non-food item with container/box/carton info:
  - Container = box/pack name
  - Count if stated, else null

----------------------------------------------------------------
## G) MAKRO TAX CATEGORY MAPPING (digits or letters)

22. In the Iva column, tax may be encoded as digits or letters.
    First apply fixed MAKRO mapping when code is numeric:
      2 = 23.00% → Normal
      4 = 6.00%  → Reduced
      5 = 13.00% → Intermedia
23. If code is not in {2,4,5} OR is a letter,
    find the decoding in the TaxCategoriesRows / tax legend after products.
    Use that legend to map code → category name or rate.
24. If still unclear, keep raw code as TaxCategory.

----------------------------------------------------------------
## H) OCR FIXES AND RECOVERY

25. Correct trivial OCR mistakes only when safe.
26. Prefer inclusion: ambiguous lines attach to nearest plausible product (usually
    as continuation of the previous product) rather than being discarded.

----------------------------------------------------------------
## I) OUTPUT (Invoice Schema in the response_format section.)

Return strictly JSON matching the response Invoice Schema:

{
  ""Products"": [
    {
      ""ProductCode"": string,
      ""ProductName"": string,
      ""Unit"": string,
      ""Quantity"": number,
      ""Container"": string,
      ""Count"": number|null,
      ""ProductTotalValue"": number,
      ""TaxCategory"": string
    }
  ],
  ""Comments"": string|null
}

Rules:
- Output ALL products found on the page, including those that received discounts.
- Never omit a product line.
- Never change the order of product rows: preserve the same top→bottom sequence as on the invoice page.
- `ProductTotalValue` must represent the final net amount per product line
  (Valor Total minus allocated discount share, if any).
- If legend decoding or total cross-check is needed, you may describe it briefly in Comments.
";


    }
}


