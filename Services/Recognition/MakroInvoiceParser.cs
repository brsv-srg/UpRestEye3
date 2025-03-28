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
        private static readonly List<SimplifiedHeader> TableHeaders = new List<SimplifiedHeader>
        {
            new SimplifiedHeader { HeaderText = "Código Artigo", Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "Descrição Artigo" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "PACK" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "PR Unit/KG" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "Unit/KG" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "Preço U.V." , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "Quant" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "Valor Total" , Words = new List<SimplifiedWord>() },
            new SimplifiedHeader { HeaderText = "IvaDD" , Words = new List<SimplifiedWord>() }
        };

        public InvoiceDTO ParseInvoice(ResortedSimplifiedDocument document)
        {
            var invoice = new InvoiceDTO();
            var isFinded = FindTableHeaders(document);
            if (!isFinded)
            {
                throw new Exception("Table headers not found.");
            }
            var skewAngle = CalculateSkewAngle();
            ExtendHeaders();
            // var numberOfItems = FindNumberOfItems(document);
            var productRows = FindProductRows(document);
            ValidateRowsAndColumns(productRows);

            // Populate invoice with parsed data
            invoice.Products = productRows.Select(row => new InvoiceProductDTO
            {
                // Map row data to InvoiceProductDTO properties
            }).ToList();

            return invoice;
        }

//        private bool FindTableHeaders(SimplifiedDocument document)
//        {
//            // Step 2: Find all table headers in the document
//            bool isAllFound = true;
//            bool isCurrentFound = false;
//            SimplifiedWord prevHeader = null;
//            foreach (var header in TableHeaders)
//            {
//                var test = Regex.Split("Preço U.V.", @"\s+|(?<=\p{P}(?!\.))");

//                var headerParts = Regex.Split(header.WordText, @"\s+|(?=\p{P}(?!\.))|(?<=\p{P}(?!\.))");
//                //var headerParts = Regex.Split(header.WordText, @"\s+|(?=\p{P})|(?<=\p{P})");

//                var matchedWords = new List<SimplifiedWord>();
//                int i = 0;
//                int startPart = 0;
//                bool isFirst = false;
//                bool isLast = false;
//                foreach (var page in document.Pages)
//                {
//                    foreach (var block in page.Blocks)
//                    {
//                        foreach (var paragraph in block.Paragraphs)
//                        {
//                            for (i = startPart; i < headerParts.Count(); i++)
//                            {
//                                foreach (var word in paragraph.Words)
//                                {
//                                    if (IsFuzzyMatch(word.WordText, headerParts[i], headerParts.Count() == 1)
//                                        &&
//                                        (matchedWords.Count() == 0 && AreWordsInLine(prevHeader, word) || 
//                                            matchedWords.Count() != 0 && AreWordsAdjacent(matchedWords.Last(), word)))
//                                    {
//                                        matchedWords.Add(word);
                                        
//                                        if (i == 0)
//                                            isFirst = true;
                                        
//                                        if (i == headerParts.Count() - 1)
//                                            isLast = true;
                                        
//                                        startPart = ++i;
//                                        if (i >= headerParts.Count())
//                                            break;
//                                    }
//                                }
//                            }

//                            if (headerParts.Count() == matchedWords.Count() || isFirst && isLast)
//                            {
//                                var combinedCoordinates = CombineCoordinates(matchedWords);
//                                header.WordCoordinates = combinedCoordinates;
//                                isCurrentFound = true;
//                                matchedWords.Clear();
//                                break;
//                            }
                            
//                        }
//                        if (isCurrentFound)
//                            break;
//                    }
//                    if (isCurrentFound)
//                        break;
//                }
//                if (!isCurrentFound)
//                    isAllFound = false;
//                else
//                    isCurrentFound = false;
//                prevHeader = header;
//            }
//            return isAllFound;
//        }
//


        public bool FindTableHeaders(ResortedSimplifiedDocument document)
        {
           
            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var line in block.Rows)
                    {
                        var combinedWords = new StringBuilder();
                        var matchedWords = new List<SimplifiedWord>();
                        var previousMatchedWords = new List<SimplifiedWord>();

                        foreach (var word in line.Words)
                        {
                            combinedWords.Append(word.WordText + " ");
                            matchedWords.Add(word);

                            var combinedText = combinedWords.ToString().Trim();

                            bool isMatchFound = false;

                            foreach (var header in TableHeaders)
                            {
                                var matchType = IsFuzzyMatch(combinedText, header.HeaderText);
                                if (matchType == MatchType.FullMatch)
                                {
                                    
                                    header.Words.AddRange(matchedWords);
                                    combinedWords.Clear();
                                    matchedWords.Clear();
                                    isMatchFound = true;
                                    break;
                                }
                                else if (matchType == MatchType.PartialMatch)
                                {
                                    previousMatchedWords.AddRange(matchedWords);
                                    matchedWords.Clear();
                                    break;
                                }
                            }

                            if (!isMatchFound)
                            {
                                if (previousMatchedWords.Count > 0)
                                {
                                    combinedWords.Clear();
                                    matchedWords.Clear();
                                    combinedWords.Append(word.WordText + " ");
                                    matchedWords.Add(word);

                                    foreach (var header in TableHeaders)
                                    {
                                        var matchType = IsFuzzyMatch(word.WordText, header.HeaderText);
                                        if (matchType == MatchType.PartialMatch)
                                        {
                                            previousMatchedWords.Clear();
                                            previousMatchedWords.Add(word);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    previousMatchedWords.Clear();
                                    previousMatchedWords.Add(word);
                                }
                            }
                        }
                    }
                }
            }
            bool isFounded = true;
            foreach (var header in TableHeaders)
            {
                if (header.Words.Count == 0)
                    isFounded = false;
                else
                    header.HeaderCoordinates = CombineCoordinates(header.Words);
            }

            return isFounded;
        }




        private bool AreWordsAdjacent(SimplifiedWord currentWord, SimplifiedWord nextWord)
        {
            // Check if the words are adjacent based on their coordinates
           return nextWord.WordCoordinates.TopLeft.X >= currentWord.WordCoordinates.TopRight.X &&
                   nextWord.WordCoordinates.TopLeft.X - currentWord.WordCoordinates.TopRight.X <= 10 &&
                   Math.Abs(currentWord.WordCoordinates.TopRight.Y - nextWord.WordCoordinates.TopLeft.Y) < 10;
        }
        private bool AreWordsInLine(SimplifiedWord currentWord, SimplifiedWord nextWord)
        {
            if (currentWord == null)
                return true;
            // Check if the words are on the same level based on their Y coordinates

            return nextWord.WordCoordinates.TopLeft.X >= currentWord.WordCoordinates.TopRight.X &&
                    Math.Abs(currentWord.WordCoordinates.TopRight.Y - nextWord.WordCoordinates.TopLeft.Y) < 10;
        }

        private bool IsFuzzyMatch(string word, string header, bool isSingle = true)
        {
            if(string.IsNullOrEmpty(word) || string.IsNullOrEmpty(header))
                return false;

            if(IsOnlyPunctuation(word) != IsOnlyPunctuation(header))
                return false;

            // First check: simple comparison
            if (string.Equals(word, header, StringComparison.OrdinalIgnoreCase))
                return true;

            // Second check: normalized simple comparison
            string normalizedWord = NormalizeString(word);
            string normalizedHeader = NormalizeString(header);

            if (string.IsNullOrEmpty(normalizedWord) || string.IsNullOrEmpty(normalizedHeader))
                return false;

            if (string.Equals(normalizedWord, normalizedHeader, StringComparison.OrdinalIgnoreCase))
                return true;

            // Third check: Levenshtein distance
            int distance = LevenshteinDistance(normalizedWord, normalizedHeader);
            int maxLength = Math.Max(normalizedWord.Length, normalizedHeader.Length);
            if (isSingle)
                return (double)distance / maxLength <= 0.3; // Allow up to 30% difference
            else
                return (double)distance / maxLength <= 0.5; // Allow up to 30% difference
        }
        private MatchType IsFuzzyMatch(string word, string header)
        {
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(header))
                return MatchType.NoMatch; 

            if (IsOnlyPunctuation(word) != IsOnlyPunctuation(header))
                return MatchType.NoMatch; 

            // First check: simple comparison
            if (string.Equals(word, header, StringComparison.OrdinalIgnoreCase))
            {
                return MatchType.FullMatch;
            }

            // Second check: normalized simple comparison
            string normalizedWord = NormalizeString(word);
            string normalizedHeader = NormalizeString(header);
            
            if (string.IsNullOrEmpty(normalizedWord) || string.IsNullOrEmpty(normalizedHeader))
                return MatchType.NoMatch;

            if (string.Equals(normalizedWord, normalizedHeader, StringComparison.OrdinalIgnoreCase))
                return MatchType.FullMatch;

            // Third check: Levenshtein distance
            int distance = LevenshteinDistance(normalizedWord, normalizedHeader);
            int maxLength = Math.Max(normalizedWord.Length, normalizedHeader.Length);
            if ((double)distance / maxLength < 0.3) // Allow up to 30% difference
            {
                return MatchType.FullMatch;
            }

            // Check if the header starts with the current word
            if (normalizedHeader.StartsWith(normalizedWord, StringComparison.OrdinalIgnoreCase))
            {
                return MatchType.PartialMatch;
            }

            return MatchType.NoMatch;
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
                var dx = TableHeaders[i + 1].HeaderCoordinates.TopLeft.X - TableHeaders[i].HeaderCoordinates.TopLeft.X;
                var dy = TableHeaders[i + 1].HeaderCoordinates.TopLeft.Y - TableHeaders[i].HeaderCoordinates.TopLeft.Y;
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
                extendedHeaders[currentHeader.HeaderText] = new RectangleCoordinates
                {
                    TopLeft = currentHeader.HeaderCoordinates.TopLeft,
                    TopRight = new TPoint(nextHeader.HeaderCoordinates.TopLeft.X - 1, currentHeader.HeaderCoordinates.TopRight.Y),
                    BottomRight = new TPoint(nextHeader.HeaderCoordinates.BottomLeft.X - 1, currentHeader.HeaderCoordinates.BottomRight.Y),
                    BottomLeft = currentHeader.HeaderCoordinates.BottomLeft
                };
            }
            var lastHeader = TableHeaders.Last();
            extendedHeaders[lastHeader.HeaderText] = new RectangleCoordinates
            {
                TopLeft = lastHeader.HeaderCoordinates.TopLeft,
                TopRight = new TPoint(lastHeader.HeaderCoordinates.TopRight.X + 100, lastHeader.HeaderCoordinates.TopRight.Y), // Arbitrary extension for the last header
                BottomRight = new TPoint(lastHeader.HeaderCoordinates.BottomRight.X + 100, lastHeader.HeaderCoordinates.BottomRight.Y),
                BottomLeft = lastHeader.HeaderCoordinates.BottomLeft
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

        private List<List<string>> FindProductRows(ResortedSimplifiedDocument document, int numberOfItems = -1)
        {
            // Step 6: Find all product rows
            var productRows = new List<List<string>>();
            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var line in block.Rows)
                    {
                        var row = new List<string>();
                        foreach (var word in line.Words)
                        {
                            foreach (var header in TableHeaders)
                            {
                                var coordinates = header.HeaderCoordinates;
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
            if (productRows.Count != numberOfItems || numberOfItems == -1)
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
        private bool IsOnlyPunctuation(string input)
        {
            return input.All(char.IsPunctuation);
        }
    }


    public enum MatchType
    {
        NoMatch,
        PartialMatch,
        FullMatch
    }

}
