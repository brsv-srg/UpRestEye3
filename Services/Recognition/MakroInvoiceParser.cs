using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;

// TODO сделать как сервис  
namespace UpRestEye3.Services.Recognition
{
    public class MakroInvoiceParser
    {
        private static readonly List<SimplifiedWord> TableHeaders = new List<SimplifiedWord>
        {
            new SimplifiedWord { WordText = "Código Artigo" },
            new SimplifiedWord { WordText = "Descrição Artigo" },
            new SimplifiedWord { WordText = "PACK" },
            new SimplifiedWord { WordText = "PR Unit/KG" },
            new SimplifiedWord { WordText = "Unit/KG" },
            new SimplifiedWord { WordText = "Preço U.V." },
            new SimplifiedWord { WordText = "Quant" },
            new SimplifiedWord { WordText = "Valor Total" },
            new SimplifiedWord { WordText = "IvaDD" }
        };

        public InvoiceDTO ParseInvoice(SimplifiedDocument document)
        {
            var invoice = new InvoiceDTO();
            var isFinded = FindTableHeaders(document);
            if (!isFinded)
            {
                throw new Exception("Table headers not found.");
            }
            var skewAngle = CalculateSkewAngle();
            ExtendHeaders();
            var numberOfItems = FindNumberOfItems(document);
            var productRows = FindProductRows(document, numberOfItems);
            ValidateRowsAndColumns(productRows);

            // Populate invoice with parsed data
            invoice.Products = productRows.Select(row => new InvoiceProductDTO
            {
                // Map row data to InvoiceProductDTO properties
            }).ToList();

            return invoice;
        }

        private bool FindTableHeaders(SimplifiedDocument document)
        {
            // Step 2: Find all table headers in the document
            bool isAllFound = true;
            bool isCurrentFound = false;
            foreach (var header in TableHeaders)
            {
                var headerParts = Regex.Split(header.WordText, @"\s+|(?=\p{P})|(?<=\p{P})");
                var matchedWords = new List<SimplifiedWord>();
                int i = 0;
                int startPart = 0;
                foreach (var page in document.Pages)
                {
                    foreach (var block in page.Blocks)
                    {
                        foreach (var paragraph in block.Paragraphs)
                        {
                            for (i = startPart; i < headerParts.Count(); i++)
                            {
                                foreach (var word in paragraph.Words)
                                {
                                    if (IsFuzzyMatch(word.WordText, headerParts[i])
                                        &&
                                        (matchedWords.Count() == 0 || matchedWords.Count() != 0 && AreWordsAdjacent(matchedWords.Last(), word)))
                                    {
                                        matchedWords.Add(word);
                                        startPart = ++i;
                                        if (i >= headerParts.Count())
                                            break;
                                    }
                                }
                            }

                            if (headerParts.Count() == matchedWords.Count())
                            {
                                var combinedCoordinates = CombineCoordinates(matchedWords);
                                header.WordCoordinates = combinedCoordinates;
                                isCurrentFound = true;
                                matchedWords.Clear();
                                break;
                            }
                            
                        }
                        if (isCurrentFound)
                            break;
                    }
                    if (isCurrentFound)
                        break;
                }
                if (!isCurrentFound)
                    isAllFound = false;
                else
                    isCurrentFound = false;
            }
            return isAllFound;
        }

        private bool AreWordsAdjacent(SimplifiedWord currentWord, SimplifiedWord nextWord)
        {
            // Check if the words are adjacent based on their coordinates
            return nextWord.WordCoordinates.TopLeft.X > currentWord.WordCoordinates.TopRight.X &&
                   nextWord.WordCoordinates.TopLeft.X - currentWord.WordCoordinates.TopRight.X <= 10 &&
                   Math.Abs(currentWord.WordCoordinates.TopRight.Y - nextWord.WordCoordinates.TopLeft.Y) < 10;
        }

        private bool IsFuzzyMatch(string word, string header)
        {
            // First check: simple comparison
            if (string.Equals(word, header, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Second check: normalized simple comparison
            string normalizedWord = NormalizeString(word);
            string normalizedHeader = NormalizeString(header);
            if (string.Equals(normalizedWord, normalizedHeader, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Third check: Levenshtein distance
            int distance = LevenshteinDistance(normalizedWord, normalizedHeader);
            int maxLength = Math.Max(normalizedWord.Length, normalizedHeader.Length);
            return (double)distance / maxLength <= 0.5; // Allow up to 30% difference
        }

        private string NormalizeString(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark && !char.IsPunctuation(c) && !char.IsSymbol(c))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] costs = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                costs[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                costs[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    costs[i, j] = Math.Min(
                        Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1),
                        costs[i - 1, j - 1] + cost);
                }
            }

            return costs[a.Length, b.Length];
        }

        private RectangleCoordinates CombineCoordinates(List<SimplifiedWord> words)
        {
            var topLeftX = words.Min(w => w.WordCoordinates.TopLeft.X);
            var topLeftY = words.Min(w => w.WordCoordinates.TopLeft.Y);
            var bottomRightX = words.Max(w => w.WordCoordinates.BottomRight.X);
            var bottomRightY = words.Max(w => w.WordCoordinates.BottomRight.Y);

            return new RectangleCoordinates
            {
                TopLeft = new TPoint(topLeftX, topLeftY),
                TopRight = new TPoint(bottomRightX, topLeftY),
                BottomRight = new TPoint(bottomRightX, bottomRightY),
                BottomLeft = new TPoint(topLeftX, bottomRightY)
            };
        }

        private double CalculateSkewAngle()
        {
            // Step 3: Calculate skew angle between headers
            var angles = new List<double>();
            for (int i = 0; i < TableHeaders.Count - 1; i++)
            {
                var dx = TableHeaders[i + 1].WordCoordinates.TopLeft.X - TableHeaders[i].WordCoordinates.TopLeft.X;
                var dy = TableHeaders[i + 1].WordCoordinates.TopLeft.Y - TableHeaders[i].WordCoordinates.TopLeft.Y;
                angles.Add(Math.Atan2(dy, dx) * (180 / Math.PI));
            }
            return angles.Average();
        }

        private void ExtendHeaders()
        {
            // Step 4: Extend headers
            var extendedHeaders = new Dictionary<string, RectangleCoordinates>();
            for (int i = 0; i < TableHeaders.Count - 1; i++)
            {
                var currentHeader = TableHeaders[i];
                var nextHeader = TableHeaders[i + 1];
                extendedHeaders[currentHeader.WordText] = new RectangleCoordinates
                {
                    TopLeft = currentHeader.WordCoordinates.TopLeft,
                    TopRight = new TPoint(nextHeader.WordCoordinates.TopLeft.X - 1, currentHeader.WordCoordinates.TopRight.Y),
                    BottomRight = new TPoint(nextHeader.WordCoordinates.BottomLeft.X - 1, currentHeader.WordCoordinates.BottomRight.Y),
                    BottomLeft = currentHeader.WordCoordinates.BottomLeft
                };
            }
            var lastHeader = TableHeaders.Last();
            extendedHeaders[lastHeader.WordText] = new RectangleCoordinates
            {
                TopLeft = lastHeader.WordCoordinates.TopLeft,
                TopRight = new TPoint(lastHeader.WordCoordinates.TopRight.X + 100, lastHeader.WordCoordinates.TopRight.Y), // Arbitrary extension for the last header
                BottomRight = new TPoint(lastHeader.WordCoordinates.BottomRight.X + 100, lastHeader.WordCoordinates.BottomRight.Y),
                BottomLeft = lastHeader.WordCoordinates.BottomLeft
            };
            return;
        }

        private int FindNumberOfItems(SimplifiedDocument document)
        {
            // Step 5: Find the number of items
            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        var words = paragraph.Words.Select(w => w.WordText).ToList();
                        if (words.Contains("N° de artigos:"))
                        {
                            var index = words.IndexOf("N° de artigos:") + 1;
                            if (index < words.Count)
                            {
                                if (int.TryParse(words[index], out int numberOfItems))
                                {
                                    return numberOfItems;
                                }
                            }
                        }
                    }
                }
            }
            throw new Exception("Number of items not found.");
        }

        private List<List<string>> FindProductRows(SimplifiedDocument document, int numberOfItems)
        {
            // Step 6: Find all product rows
            var productRows = new List<List<string>>();
            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        var row = new List<string>();
                        foreach (var word in paragraph.Words)
                        {
                            foreach (var header in TableHeaders)
                            {
                                var coordinates = header.WordCoordinates;
                                if (IsWithinCoordinates(word.WordCoordinates, coordinates))
                                {
                                    row.Add(word.WordText);
                                }
                            }
                        }
                        if (row.Count == TableHeaders.Count)
                        {
                            productRows.Add(row);
                        }
                    }
                }
            }
            if (productRows.Count != numberOfItems)
            {
                throw new Exception("Mismatch in number of product rows.");
            }
            return productRows;
        }

        private bool IsWithinCoordinates(RectangleCoordinates wordCoordinates, RectangleCoordinates headerCoordinates)
        {
            // Check if word coordinates are within header coordinates
            return wordCoordinates.TopLeft.X >= headerCoordinates.TopLeft.X &&
                   wordCoordinates.TopRight.X <= headerCoordinates.TopRight.X &&
                   wordCoordinates.TopLeft.Y >= headerCoordinates.TopLeft.Y &&
                   wordCoordinates.BottomLeft.Y <= headerCoordinates.BottomLeft.Y;
        }

        private void ValidateRowsAndColumns(List<List<string>> productRows)
        {
            // Step 7: Validate rows and columns
            foreach (var row in productRows)
            {
                if (row.Count != TableHeaders.Count)
                {
                    throw new Exception("Mismatch in number of columns.");
                }
            }
        }
    }
}
