using System;
using System.IO;
using System.Text;
using UpRestEye3.Models.BLO;
using Google.Cloud.Vision.V1;

namespace UpRestEye3.Services.Recognition
{
    public class HtmlGenerator
    {
        public string GenerateHtmlFromResortedSimplifiedDocument(ResortedSimplifiedDocument document)
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body style='position: relative; font-family: monospace; font-size: 8px;'>");

            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var line in block.Rows)
                    {
                        foreach (var word in line.Words)
                        {
                            var style = $"position: absolute; left: {word.WordCoordinates.TopLeft.X}px; top: {word.WordCoordinates.TopLeft.Y}px;";
                            htmlBuilder.Append($"<span style='{style}'>{word.WordText}</span>");
                        }
                    }
                }
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }

        public string GenerateHtmlFromTextAnnotation(TextAnnotation textAnnotation)
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body style='position: relative; font-family: monospace; font-size: 8px;'>");

            foreach (var page in textAnnotation.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        foreach (var word in paragraph.Words)
                        {
                            var topLeft = word.BoundingBox.Vertices[0];
                            var style = $"position: absolute; left: {topLeft.X}px; top: {topLeft.Y}px;";
                            var wordText = string.Join("", word.Symbols.Select(s => s.Text));
                            htmlBuilder.Append($"<span style='{style}'>{wordText}</span>");
                        }
                    }
                }
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }

        public void SaveHtmlToFile(string htmlContent, string fileName)
        {
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var filePath = Path.Combine(downloadsPath, fileName);

            File.WriteAllText(filePath, htmlContent);
        }

        public string GenerateHtmlIgnoringVerticalCoordinates(ResortedSimplifiedDocument document)
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body style='font-family: monospace; font-size: 8px;'>");

            foreach (var page in document.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var line in block.Rows)
                    {
                        foreach (var word in line.Words)
                        {
                            var style = $"position: relative; left: {word.WordCoordinates.TopLeft.X}px;";
                            htmlBuilder.Append($"<span style='{style}'>{word.WordText}</span>");
                        }
                        htmlBuilder.Append("<br/>");
                        htmlBuilder.Append(new string('-', 100));
                        htmlBuilder.Append("<br/>");
                    }
                }
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }

    }
}

