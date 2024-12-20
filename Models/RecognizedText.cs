namespace UpRestEye3.Models
{
    [Serializable]
    public class RecognizedDocument
    {
        public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();
    }
    
    public class TextBlock
    {
        public int BlockNumber { get; set; }
        public string BlockCoordinates { get; set; }
        public List<TextParagraph> Paragraphs { get; set; } = new List<TextParagraph>();
    }

    public class TextParagraph
    {
        public int ParagraphNumber { get; set; }
        public string ParagraphCoordinates { get; set; }
        public string ParagraphText { get; set; }
    }
}
