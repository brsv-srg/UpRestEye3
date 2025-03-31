using UpRestEye3.Services.BusinessLogic;
using System.Text.Json.Serialization; 

namespace UpRestEye3.Models.BLO
{
    [Serializable]
    public class RecognizedDocument
    {
        public List<TextBlock> TextBlocks { get; set; } = [];
    }

    public class TextBlock
    {
        public int BlockNumber { get; set; }
        public string BlockCoordinates { get; set; } = string.Empty;
        public List<TextParagraph> Paragraphs { get; set; } = [];
    }

    public class TextParagraph
    {
        public int ParagraphNumber { get; set; }
        public string ParagraphCoordinates { get; set; } = string.Empty;
        public string ParagraphText { get; set; } = string.Empty;
    }

    public class SimplifiedDocument
    {
        public List<SimplifiedPage> Pages { get; set; }
    }

    public class ResortedSimplifiedDocument
    {
        public List<SimplifiedRowPage> Pages { get; set; }
    }

    public class TablesDataDocument
    {
        public List<SimplifiedRow> ProductHeaders { get; set; }
        public List<SimplifiedRow> ProductRows { get; set; }
        public List<SimplifiedRow> TaxCategoriesHeaders { get; set; }
        public List<SimplifiedRow> TaxCategoriesRows { get; set; }
    }

    public class SimplifiedPage
    {
        public List<SimplifiedBlock> Blocks { get; set; }
    }
    public class SimplifiedRowPage
    {
        public List<SimplifiedLinesBlock> Blocks { get; set; }
    }

    public class SimplifiedBlock
    {
        public RectangleCoordinates BlockCoordinates { get; set; }
        public List<SimplifiedParagraph> Paragraphs { get; set; }
    }
    public class SimplifiedLinesBlock
    {
        public RectangleCoordinates BlockCoordinates { get; set; }
        public List<SimplifiedRow> Rows { get; set; }
    }
    public class SimplifiedParagraph
    {
        public RectangleCoordinates ParagraphCoordinates { get; set; }
        public List<SimplifiedWord> Words { get; set; }
    }

    public class SimplifiedRow
    {
        public RectangleCoordinates RowCoordinates { get; set; }
        public List<SimplifiedWord> Words { get; set; }
    }

    public class SimplifiedWord
    {
        public string WordText { get; set; }
        public RectangleCoordinates WordCoordinates { get; set; }
    }

    public class SimplifiedHeader
    {
        public string HeaderText { get; set; }
        public RectangleCoordinates HeaderCoordinates { get; set; }
        public List<SimplifiedWord> Words { get; set; }
    }

    [JsonConverter(typeof(RectangleCoordinatesConverter))]
    public struct RectangleCoordinates
    {
        public TPoint TopLeft { get; set; }
        public TPoint TopRight { get; set; }
        public TPoint BottomRight { get; set; }
        public TPoint BottomLeft { get; set; }
    }

    public struct TPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public TPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
