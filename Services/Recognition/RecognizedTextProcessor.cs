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
            var lines = GroupWordsIntoLines(words, averageWordHeight);
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
            return words;
        }

        private double GetAverageWordHeight(List<SimplifiedWord> words)
        {
            return words.Average(w => w.WordCoordinates.BottomLeft.Y - w.WordCoordinates.TopLeft.Y);
        }

        private List<SimplifiedRow> GroupWordsIntoLines(List<SimplifiedWord> words, double averageWordHeight)
        {
            var lines = new List<SimplifiedRow>();
            words = words.OrderBy(w => w.WordCoordinates.TopLeft.Y).ThenBy(w => w.WordCoordinates.TopLeft.X).ToList();

            var currentLine = new SimplifiedRow { Words = new List<SimplifiedWord>() };

            foreach (var word in words)
            {
               if (currentLine.Words.Count == 0 || IsSameLine(currentLine, word, averageWordHeight))
                {
                    currentLine.Words.Add(word);
                }
                else
                {
                    currentLine.Words = currentLine.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).ToList(); // Sort words in the line by X coordinate
                    currentLine.RecalculateCoordinates();
                    lines.Add(currentLine);
                    currentLine = new SimplifiedRow { Words = new List<SimplifiedWord> { word } };
                }
            }
            if (currentLine.Words.Count > 0)
            {
                currentLine.Words = currentLine.Words.OrderBy(w => w.WordCoordinates.TopLeft.X).ToList(); // Sort words in the line by X coordinate
                currentLine.RecalculateCoordinates();
                lines.Add(currentLine);
            }

            return lines;
        }

        //private bool IsSameLine(SimplifiedWord lastWord, SimplifiedWord currentWord, double averageWordHeight)
        //{
        //    double verticalDistance = Math.Abs(currentWord.WordCoordinates.TopLeft.Y - lastWord.WordCoordinates.BottomLeft.Y);
        //    return verticalDistance < averageWordHeight / 2;
        //}


        //private bool IsSameLine(SimplifiedLine currentLine, SimplifiedWord currentWord, double averageWordHeight)
        //{
        //    // Check if the current word is within the Y range of the current line
        //    double lineTopY = currentLine.Words.Min(w => w.WordCoordinates.TopLeft.Y);
        //    double lineBottomY = currentLine.Words.Max(w => w.WordCoordinates.BottomLeft.Y);
        //    double wordTopY = currentWord.WordCoordinates.TopLeft.Y;
        //    double wordBottomY = currentWord.WordCoordinates.BottomLeft.Y;

        //    return (wordTopY >= lineTopY - averageWordHeight / 2 && wordTopY <= lineBottomY + averageWordHeight / 2) ||
        //           (wordBottomY >= lineTopY - averageWordHeight / 2 && wordBottomY <= lineBottomY + averageWordHeight / 2);
        //}


        private bool IsSameLine(SimplifiedRow currentLine, SimplifiedWord currentWord, double averageWordHeight)
        {
            // Check if the current word is within the Y range of the current line
            double lineTopY = currentLine.Words.Min(w => w.WordCoordinates.TopLeft.Y);
            double lineBottomY = currentLine.Words.Max(w => w.WordCoordinates.BottomLeft.Y);
            double wordTopY = currentWord.WordCoordinates.TopLeft.Y;
            double wordBottomY = currentWord.WordCoordinates.BottomLeft.Y;

            double upperTolerance = averageWordHeight / 4;
            double lowerTolerance = averageWordHeight / 4;

            bool isWithinUpperRange = wordTopY >= lineTopY - upperTolerance && wordTopY <= lineBottomY + upperTolerance;
            bool isWithinLowerRange = wordBottomY >= lineTopY - lowerTolerance && wordBottomY <= lineBottomY + lowerTolerance;

            bool isSameLine = isWithinUpperRange || isWithinLowerRange;

            return isSameLine;
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
