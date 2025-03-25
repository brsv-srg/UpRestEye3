using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace UpRestEye3.Services.Recognition
{
    public class TextRecognitionHelper
    {

        public class EnhancedTextBlock
        {
            public string Text { get; set; }
            public BoundingPoly Bounds { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string ProbableDataType { get; set; }
            public int RowIndex { get; set; } = -1;
            public int ColIndex { get; set; } = -1;
            public double FontSize { get; set; }
        }

        /// <summary>
        /// Предварительная обработка результатов OCR с вычислением дополнительных метаданных
        /// </summary>
        private static List<EnhancedTextBlock> PreprocessOCRResults(TextAnnotation ocrResult)
        {
            var enhancedBlocks = new List<EnhancedTextBlock>();

            foreach (var page in ocrResult.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        var text = string.Join(" ", paragraph.Words.Select(w => string.Join("", w.Symbols.Select(s => s.Text))));
                        var bounds = paragraph.BoundingBox;

                        // Вычисление центра блока текста
                        double centerX = bounds.Vertices.Average(v => v.X);
                        double centerY = bounds.Vertices.Average(v => v.Y);

                        // Вычисление размеров блока
                        double minX = bounds.Vertices.Min(v => v.X);
                        double maxX = bounds.Vertices.Max(v => v.X);
                        double minY = bounds.Vertices.Min(v => v.Y);
                        double maxY = bounds.Vertices.Max(v => v.Y);
                        double width = maxX - minX;
                        double height = maxY - minY;

                        // Оценка размера шрифта по высоте блока и количеству символов
                        double fontSize = height / paragraph.Words.Count;

                        // Определение вероятного типа данных
                        string dataType = DetermineDataType(text);

                        enhancedBlocks.Add(new EnhancedTextBlock
                        {
                            Text = text,
                            Bounds = bounds,
                            CenterX = centerX,
                            CenterY = centerY,
                            Width = width,
                            Height = height,
                            ProbableDataType = dataType,
                            FontSize = fontSize
                        });
                    }
                }
            }

            return enhancedBlocks;
        }

        /// <summary>
        /// Определение вероятного типа данных из текста
        /// </summary>
        private static string DetermineDataType(string text)
        {
            // Проверка на числовой формат (цена)
            if (decimal.TryParse(text.Replace(",", "."), out _))
                return "NUMERIC";

            // Проверка на формат даты
            if (DateTime.TryParse(text, out _))
                return "DATE";

            // Проверка на количество (число с единицами измерения)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\d+\s*(шт|кг|л|г|мл)"))
                return "QUANTITY";

            // Возможный заголовок (ключевые слова)
            string[] headerKeywords = { "товар", "наименование", "количество", "цена", "сумма", "стоимость", "итого" };
            if (headerKeywords.Any(keyword => text.ToLower().Contains(keyword)))
                return "HEADER";

            // По умолчанию - текст
            return "TEXT";
        }

        /// <summary>
        /// Группировка блоков текста по координатам для выявления строк и столбцов
        /// </summary>
        private static List<List<EnhancedTextBlock>> GroupBlocksByCoordinates(List<EnhancedTextBlock> blocks)
        {
            // Определение строк по Y-координатам (группировка блоков с близкими Y-координатами)
            var yGroups = new Dictionary<int, List<EnhancedTextBlock>>();
            double yTolerance = blocks.Average(b => b.Height) * 0.5f;

            // Сортировка блоков по Y-координате
            var sortedByY = blocks.OrderBy(b => b.CenterY).ToList();

            int currentYGroup = 0;
            double lastY = -1;

            foreach (var block in sortedByY)
            {
                if (lastY < 0 || Math.Abs(block.CenterY - lastY) > yTolerance)
                {
                    currentYGroup++;
                    lastY = block.CenterY;
                }

                if (!yGroups.ContainsKey(currentYGroup))
                    yGroups[currentYGroup] = new List<EnhancedTextBlock>();

                yGroups[currentYGroup].Add(block);
                block.RowIndex = currentYGroup;
            }

            // Сортировка каждой строки по X-координате
            foreach (var rowGroup in yGroups.Values)
            {
                var sortedRow = rowGroup.OrderBy(b => b.CenterX).ToList();
                for (int i = 0; i < sortedRow.Count; i++)
                {
                    sortedRow[i].ColIndex = i;
                }
            }

            return yGroups.Values.ToList();
        }

        /// <summary>
        /// Идентификация структуры таблицы в сгруппированных блоках
        /// </summary>
        private static TableData IdentifyTableStructure(List<List<EnhancedTextBlock>> groupedBlocks)
        {
            var tableData = new TableData();

            // Поиск строки с заголовками (обычно имеет блоки с типом HEADER)
            var headerRow = groupedBlocks.FirstOrDefault(row =>
                row.Any(block => block.ProbableDataType == "HEADER") ||
                row.Count >= 3 && row.Average(b => b.FontSize) > groupedBlocks.Average(r => r.Average(b => b.FontSize))
            );

            // Если не найдена строка с заголовками, используем первую строку
            if (headerRow == null && groupedBlocks.Count > 0)
                headerRow = groupedBlocks[0];

            if (headerRow != null)
            {
                // Заполнение заголовков
                tableData.Headers = headerRow.OrderBy(b => b.ColIndex).Select(b => b.Text).ToList();

                // Определение индекса строки с заголовками
                int headerRowIndex = headerRow.First().RowIndex;

                // Заполнение данных (строки после заголовков)
                foreach (var row in groupedBlocks.Where(r => r.First().RowIndex > headerRowIndex))
                {
                    // Создание новой строки данных
                    var dataRow = new Dictionary<string, string>();

                    // Сопоставление колонок с заголовками
                    for (int i = 0; i < Math.Min(row.Count, tableData.Headers.Count); i++)
                    {
                        var cell = row.FirstOrDefault(c => c.ColIndex == i);
                        if (cell != null)
                        {
                            var header = i < tableData.Headers.Count ? tableData.Headers[i] : $"Column{i}";
                            dataRow[header] = cell.Text;
                        }
                    }

                    if (dataRow.Count > 0)
                        tableData.Rows.Add(dataRow);
                }
            }

            return tableData;
        }

        /// <summary>
        /// Класс для представления структуры таблицы
        /// </summary>
        public class TableData
        {
            public List<string> Headers { get; set; } = new List<string>();
            public List<Dictionary<string, string>> Rows { get; set; } = new List<Dictionary<string, string>>();
        }

        /// <summary>
        /// Извлечение структурированных данных с помощью ChatGPT API
        /// </summary>
        private static async Task<string> ExtractStructuredDataWithChatGPT(TableData tableData, List<EnhancedTextBlock> enhancedBlocks)
        {
            using var client = new HttpClient();
            //client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAIApiKey}");

            // Подготовка метаданных о блоках для ChatGPT
            var blocksMetadata = enhancedBlocks.Select(b => new
            {
                text = b.Text,
                centerX = b.CenterX,
                centerY = b.CenterY,
                width = b.Width,
                height = b.Height,
                dataType = b.ProbableDataType,
                rowIndex = b.RowIndex,
                colIndex = b.ColIndex,
                fontSize = b.FontSize
            }).ToList();

            // Составление промпта для ChatGPT
            string prompt = @"
Я предоставляю данные из накладной, обработанной через OCR Google Vision. Мне нужно структурировать эти данные в формате JSON.

Текстовые блоки имеют следующие метаданные:
- text: Текст блока
- centerX, centerY: Координаты центра блока
- width, height: Размеры блока
- dataType: Вероятный тип данных (NUMERIC, DATE, QUANTITY, HEADER, TEXT)
- rowIndex, colIndex: Индексы строки и столбца в сетке
- fontSize: Приблизительный размер шрифта

Я уже идентифицировал следующую табличную структуру:
- Заголовки: " + string.Join(", ", tableData.Headers) + @"
- Количество строк данных: " + tableData.Rows.Count + @"

Используя предоставленные метаданные, проанализируй и структурируй таблицу товаров из накладной, выполнив следующие задачи:
1. Проверь и уточни заголовки таблицы
2. Проверь целостность данных в строках
3. Определи названия товаров, их количество, цены и суммы
4. Создай структурированный JSON с информацией о товарах
5. Если в блоках есть общая информация о накладной (номер, дата, продавец, покупатель), включи эту информацию в отдельный раздел JSON

Вот метаданные о текстовых блоках:
" + JsonSerializer.Serialize(blocksMetadata) + @"

Вот предварительно структурированные данные:
" + JsonSerializer.Serialize(tableData) + @"

Верни только JSON без пояснений.";

            // Создание запроса к ChatGPT API
            var requestData = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant specialized in extracting structured data from OCR results." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1
            };

            // Преобразование запроса в JSON
            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            // Отправка запроса
            var response = await client.PostAsync("", requestContent);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка API ChatGPT: {response.StatusCode}");

            // Обработка ответа
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
            string result = responseObject.choices[0].message.content.ToString();

            return result;
        }
    }
}