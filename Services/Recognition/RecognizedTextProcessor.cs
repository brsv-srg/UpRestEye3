using System;
using System.Collections.Generic;
using System.Linq;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Services.Recognition
{
    public class RecognizedTextProcessor
    {
        public ResortedSimplifiedDocument ProcessSimplifiedDocument(SimplifiedDocument document)
        {
            var words = ExtractWords(document);
            var averageWordHeight = GetAverageWordHeight(words);
            var docSlope = CalculateDocumentSlope2(words); // Новый шаг

            var lines = GroupWordsIntoLines2(words, averageWordHeight, docSlope); // Передаём наклон
            var blocks = GroupLinesIntoBlocks(lines, averageWordHeight);

            var resortedDocument = new ResortedSimplifiedDocument
            {
                Pages = new List<SimplifiedRowPage>
        {
            new SimplifiedRowPage { Blocks = blocks }
        }
            };

            return resortedDocument;
        }


        private List<SimplifiedWord> ExtractWords(SimplifiedDocument document)
        {
            var words = new List<SimplifiedWord>();
            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        words.AddRange(paragraph.Words);
                    }
                }
            }
            var averageWordHeight = GetAverageWordHeight(words);

            return words.Where(word => !IsVerticalText(word, averageWordHeight)).ToList();
        }

        private double GetAverageWordHeight(List<SimplifiedWord> words)
        {
            return words.Average(w => w.WordCoordinates.BottomLeft.Y - w.WordCoordinates.TopLeft.Y);
        }

        private List<SimplifiedRow> GroupWordsIntoLines(List<SimplifiedWord> words, double averageWordHeight, double docSlope)
        {
            var lines = new List<SimplifiedRow>();
            words = words.OrderBy(w => w.WordCoordinates.TopLeft.Y).ThenBy(w => w.WordCoordinates.TopLeft.X).ToList();

            var currentLine = new SimplifiedRow { Words = new List<SimplifiedWord>() };

            foreach (var word in words)
            {
                if (currentLine.Words.Count == 0 || IsSameLineWithSlope2(currentLine, word, averageWordHeight, docSlope))
                {
                    currentLine.Words.Add(word);
                }
                else
                {
                    currentLine.Words = currentLine.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).ToList();
                    currentLine.RecalculateCoordinates();
                    lines.Add(currentLine);
                    currentLine = new SimplifiedRow { Words = new List<SimplifiedWord> { word } };
                }
            }
            if (currentLine.Words.Count > 0)
            {
                currentLine.Words = currentLine.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).ToList();
                currentLine.RecalculateCoordinates();
                lines.Add(currentLine);
            }

            return lines;
        }

        private List<SimplifiedRow> GroupWordsIntoLines2(List<SimplifiedWord> words, double averageWordHeight, double docSlope)
        {
            var lines = new List<SimplifiedRow>();
            words = words.OrderBy(w => w.WordCoordinates.TopLeft.Y).ThenBy(w => w.WordCoordinates.TopLeft.X).ToList();

            foreach (var word in words)
            {
                bool added = false;

                // Сначала пробуем добавить к текущей (последней) строке
                if (lines.Count > 0 && IsSameLineWithSlope2(lines.Last(), word, averageWordHeight, docSlope))
                {
                    lines.Last().Words.Add(word);
                    added = true;
                }
                else
                {
                    // Если не подошло — ищем среди всех предыдущих строк
                    foreach (var line in lines)
                    {
                        if (IsSameLineWithSlope2(line, word, averageWordHeight, docSlope))
                        {
                            line.Words.Add(word);
                            added = true;
                            break;
                        }
                    }
                }

                // Если не подошло ни к одной строке — создаём новую
                if (!added)
                {
                    lines.Add(new SimplifiedRow { Words = new List<SimplifiedWord> { word } });
                }
            }

            // Пересчитываем координаты строк
            foreach (var line in lines)
            {
                line.Words = line.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).ToList();
                line.RecalculateCoordinates();
            }

            return lines;
        }



        private double CalculateDocumentSlope(List<SimplifiedWord> words)
        {
            if (words.Count < 2)
                return 0;

            var xs = words.Select(w => w.WordCoordinates.TopLeft.X).ToArray();
            var ys = words.Select(w => w.WordCoordinates.TopLeft.Y).ToArray();
            double avgX = xs.Average();
            double avgY = ys.Average();
            double numerator = 0, denominator = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                numerator += (xs[i] - avgX) * (ys[i] - avgY);
                denominator += (xs[i] - avgX) * (xs[i] - avgX);
            }
            return denominator == 0 ? 0 : numerator / denominator;
        }


        private double CalculateDocumentSlope2(List<SimplifiedWord> words)
        {
            // Фильтруем слова с экстремальными X и Y (например, 5-й и 95-й перцентили)
            var xs = words.Select(w => w.WordCoordinates.TopLeft.X).OrderBy(x => x).ToArray();
            var ys = words.Select(w => w.WordCoordinates.TopLeft.Y).OrderBy(y => y).ToArray();
            int n = xs.Length;
            int low = n / 20; // 5%
            int high = n - low;

            var filteredWords = words
                .Where(w =>
                    w.WordCoordinates.TopLeft.X >= xs[low] && w.WordCoordinates.TopLeft.X <= xs[high - 1] &&
                    w.WordCoordinates.TopLeft.Y >= ys[low] && w.WordCoordinates.TopLeft.Y <= ys[high - 1])
                .ToList();

            if (filteredWords.Count < 2)
                return 0;

            var fx = filteredWords.Select(w => w.WordCoordinates.TopLeft.X).ToArray();
            var fy = filteredWords.Select(w => w.WordCoordinates.TopLeft.Y).ToArray();
            double avgX = fx.Average();
            double avgY = fy.Average();
            double numerator = 0, denominator = 0;
            for (int i = 0; i < fx.Length; i++)
            {
                numerator += (fx[i] - avgX) * (fy[i] - avgY);
                denominator += (fx[i] - avgX) * (fx[i] - avgX);
            }
            return denominator == 0 ? 0 : numerator / denominator;
        }


        private bool IsSameLineWithSlope(SimplifiedRow currentLine, SimplifiedWord currentWord, double averageWordHeight, double docSlope)
        {
            // "Выровненные" Y-координаты
            double wordX = currentWord.WordCoordinates.TopLeft.X;
            double wordY = currentWord.WordCoordinates.TopLeft.Y - docSlope * wordX;

            var lineYs = currentLine.Words
                .Select(w => w.WordCoordinates.TopLeft.Y - docSlope * w.WordCoordinates.TopLeft.X)
                .ToArray();

            double lineY = lineYs.Average();

            double tolerance = averageWordHeight * 0.5;
            return Math.Abs(wordY - lineY) < tolerance;
        }
        private bool IsSameLineWithSlope2(SimplifiedRow currentLine, SimplifiedWord currentWord, double averageWordHeight, double docSlope)
        {
            if (currentLine.Words.Count == 0)
                return true;

            // Базовое слово — самое левое
            var baseWord = currentLine.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).First();
            double baseX = baseWord.WordCoordinates.TopLeft.X;
            double baseY = baseWord.WordCoordinates.TopLeft.Y;

            double currentX = currentWord.WordCoordinates.TopLeft.X;
            double currentY = currentWord.WordCoordinates.TopLeft.Y;

            double xDiff = Math.Abs(currentX - baseX);
            double adjustedY;

            if (baseX < currentX)
                adjustedY = baseY - docSlope * xDiff;
            else
                adjustedY = baseY + docSlope * xDiff;

            double tolerance = averageWordHeight * 0.5;

            return Math.Abs(adjustedY - currentY) < tolerance;
        }

        private bool IsSameLineWithSlope3(SimplifiedRow currentLine, SimplifiedWord currentWord, double averageWordHeight, double docSlope)
        {
            // Если строка только начинается — всегда true
            if (currentLine.Words.Count == 0)
                return true;

            // Берём первое слово строки как базовую точку
            var firstWord = currentLine.Words[0];
            double baseX = firstWord.WordCoordinates.TopLeft.X;
            double baseY = firstWord.WordCoordinates.TopLeft.Y;

            // Ожидаемая Y-координата для текущего слова по линии наклона
            double expectedY = baseY + docSlope * (currentWord.WordCoordinates.TopLeft.X - baseX);

            double actualY = currentWord.WordCoordinates.TopLeft.Y;

            double tolerance = averageWordHeight * 0.5; // Можно варьировать

            return Math.Abs(actualY - expectedY) < tolerance;
        }
        private List<SimplifiedLinesBlock> GroupLinesIntoBlocks(List<SimplifiedRow> rows, double averageWordHeight)
        {
            var blocks = new List<SimplifiedLinesBlock>();
            var currentBlock = new SimplifiedLinesBlock();
            currentBlock.Rows = new List<SimplifiedRow>();

            foreach (var row in rows)
            {
                if (currentBlock.Rows.Count == 0 || IsSameBlock(currentBlock.Rows.Last(), row, averageWordHeight))
                {
                    currentBlock.Rows.Add(row);
                }
                else
                {
                    currentBlock.RecalculateCoordinates();
                    blocks.Add(currentBlock);
                    currentBlock = new SimplifiedLinesBlock { Rows = new List<SimplifiedRow> { row } };
                }
            }
            if (currentBlock.Rows.Count > 0)
            {
                currentBlock.RecalculateCoordinates();
                blocks.Add(currentBlock);
            }

            return blocks;
        }

        private bool IsSameBlock(SimplifiedRow lastLine, SimplifiedRow currentLine, double averageWordHeight)
        {
            double verticalDistance = currentLine.Words.First().WordCoordinates.TopLeft.Y - lastLine.Words.Last().WordCoordinates.BottomLeft.Y;
            return verticalDistance < averageWordHeight*1.5;
        }

        private bool IsVerticalText(SimplifiedWord word, double averageWordHeight)
        {
            double wordHeight = word.WordCoordinates.BottomLeft.Y - word.WordCoordinates.TopLeft.Y;
            double wordWidth = word.WordCoordinates.TopRight.X - word.WordCoordinates.TopLeft.X;
            return wordHeight > wordWidth * 2 && wordHeight > averageWordHeight * 1.5; // Height is more than twice the width and exceeds average word height
        }

    }

    public static class SimplifiedLineExtensions
    {
        public static void RecalculateCoordinates(this SimplifiedRow row)
        {
            if (row.Words.Any())
            {
                row.RowCoordinates = new RectangleCoordinates
                {
                    TopLeft = new TPoint(row.Words.Min(w => w.WordCoordinates.TopLeft.X), row.Words.Min(w => w.WordCoordinates.TopLeft.Y)),
                    TopRight = new TPoint(row.Words.Max(w => w.WordCoordinates.TopRight.X), row.Words.Min(w => w.WordCoordinates.TopRight.Y)),
                    BottomRight = new TPoint(row.Words.Max(w => w.WordCoordinates.BottomRight.X), row.Words.Max(w => w.WordCoordinates.BottomRight.Y)),
                    BottomLeft = new TPoint(row.Words.Min(w => w.WordCoordinates.BottomLeft.X), row.Words.Max(w => w.WordCoordinates.BottomLeft.Y))
                };
            }
        }
    }

    public static class SimplifiedLinesBlockExtensions
    {
        public static void RecalculateCoordinates(this SimplifiedLinesBlock block)
        {
            if (block.Rows.Any())
            {
                block.BlockCoordinates = new RectangleCoordinates
                {
                    TopLeft = new TPoint(block.Rows.Min(l => l.RowCoordinates.TopLeft.X), block.Rows.Min(l => l.RowCoordinates.TopLeft.Y)),
                    TopRight = new TPoint(block.Rows.Max(l => l.RowCoordinates.TopRight.X), block.Rows.Min(l => l.RowCoordinates.TopRight.Y)),
                    BottomRight = new TPoint(block.Rows.Max(l => l.RowCoordinates.BottomRight.X), block.Rows.Max(l => l.RowCoordinates.BottomRight.Y)),
                    BottomLeft = new TPoint(block.Rows.Min(l => l.RowCoordinates.BottomLeft.X), block.Rows.Max(l => l.RowCoordinates.BottomLeft.Y))
                };
            }
        }
    }
}
