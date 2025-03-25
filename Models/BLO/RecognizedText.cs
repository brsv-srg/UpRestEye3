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

    public class SimplifiedPage
    {
        public List<SimplifiedBlock> Blocks { get; set; }
    }

    public class SimplifiedBlock
    {
        public RectangleCoordinates BlockCoordinates { get; set; }
        public List<SimplifiedParagraph> Paragraphs { get; set; }
    }

    public class SimplifiedParagraph
    {
        public RectangleCoordinates ParagraphCoordinates { get; set; }
        public List<SimplifiedWord> Words { get; set; }
    }

    public class SimplifiedWord
    {
        public string WordText { get; set; }
        public RectangleCoordinates WordCoordinates { get; set; }
    }

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
