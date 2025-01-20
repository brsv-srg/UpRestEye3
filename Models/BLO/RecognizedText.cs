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
}
