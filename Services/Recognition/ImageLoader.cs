using ImageMagick;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.StaticFiles;
using Org.BouncyCastle.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Ghostscript.NET.Rasterizer;
using PdfSharpCore.Pdf.IO;
using System.IO;
using System.IO;
using System.Runtime.InteropServices;
using UpRestEye3.Components.Pages;

using System.Drawing.Imaging;
using Ghostscript.NET.Rasterizer;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;


public class ImageLoader
{

    public List<(Bitmap, string)> LoadImage(string _filePath)
    {
        var filePaths = _filePath.Split("; ", StringSplitOptions.RemoveEmptyEntries);
        var images = new List<(Bitmap, string)>();

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var extension = System.IO.Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".pdf":
                    images.AddRange(LoadImageFromPdf(filePath));
                    break;
                case ".heic":
                    images.Add((LoadImageFromHeic(filePath), filePath));
                    break;
                default:
                    images.Add((LoadImageFromFile(filePath), filePath));
                    break; // Added break to fix CS8070
            }
        }
        return images;
    }

    private List<(Bitmap, string)> LoadImageFromPdf(string filePath)
    {
        int dpi = 150;
        var bitmaps = new List<(Bitmap, string)>();
        using (var rasterizer = new GhostscriptRasterizer())
        {
            rasterizer.Open(filePath);
            int pageCount = rasterizer.PageCount;
            for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                var img = rasterizer.GetPage(dpi, pageNumber);
                bitmaps.Add((new Bitmap(img),filePath));
            }
        }
        return bitmaps;
    }


    private Bitmap LoadImageFromHeic(string filePath)
    {
        using (var image = new MagickImage(filePath))
        {
            using (var ms = new MemoryStream())
            {
                image.Write(ms, MagickFormat.Png);
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }
    }

    private Bitmap LoadImageFromFile_old(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            return new Bitmap(stream);
        }
    }

    public byte[] ConvertBitmapToByteArray(Bitmap bitmap)
    {
        using (var memoryStream = new MemoryStream())
        {
            // Сохраняем Bitmap в поток памяти в формате PNG (или другом формате)
            bitmap.Save(memoryStream, ImageFormat.Png);

            // Преобразуем поток в массив байтов
            return memoryStream.ToArray();
        }
    }


    private Bitmap LoadImageFromFile(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Bitmap bitmap = new Bitmap(stream);
            if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                string outputFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), System.IO.Path.GetFileNameWithoutExtension(filePath) + "_converted.png");

                // Check if the file already exists and delete it
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }

                Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                }

                newBitmap.Save(outputFilePath, ImageFormat.Png);

                using (var newStream = new FileStream(outputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return new Bitmap(newStream);
                }
            }
            return bitmap;
        }
    }

    /// <summary>
    /// Физически разделяет многостраничные PDF и TIFF на отдельные файлы, возвращает строку с новыми путями.
    /// </summary>
    public string SplitMultiPageFiles(string fileList)
    {
        var filePaths = fileList.Split("; ", StringSplitOptions.RemoveEmptyEntries);
        var resultFiles = new List<string>();

        foreach (var filePath in filePaths)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".pdf")
            {
                // PDF: разделить на страницы и сохранить каждую как отдельный PDF
                using (var reader = new iTextSharp.text.pdf.PdfReader(filePath))
                {
                    int pageCount = reader.NumberOfPages;
                    if (pageCount > 1)
                    {
                        for (int page = 1; page <= pageCount; page++)
                        {
                            string newFileName = System.IO.Path.Combine(
                                System.IO.Path.GetDirectoryName(filePath),
                                $"{System.IO.Path.GetFileNameWithoutExtension(filePath)} page {page}{extension}"
                            );

                            using (var doc = new iTextSharp.text.Document())
                            using (var fs = new FileStream(newFileName, FileMode.Create, FileAccess.Write))
                            {
                                var pdfCopy = new iTextSharp.text.pdf.PdfCopy(doc, fs);
                                doc.Open();
                                var importedPage = pdfCopy.GetImportedPage(reader, page);
                                pdfCopy.AddPage(importedPage);
                                doc.Close();
                            }
                            resultFiles.Add(newFileName);
                        }
                    }
                    else
                    {
                        resultFiles.Add(filePath);
                    }
                }
            }
            else if (extension == ".tif" || extension == ".tiff")
            {
                // TIFF: разделить на страницы и сохранить каждую как отдельный TIFF
                using (var image = Image.FromFile(filePath))
                {
                    int pageCount = image.GetFrameCount(FrameDimension.Page);
                    if (pageCount > 1)
                    {
                        for (int page = 0; page < pageCount; page++)
                        {
                            image.SelectActiveFrame(FrameDimension.Page, page);
                            string newFileName = System.IO.Path.Combine(
                                System.IO.Path.GetDirectoryName(filePath),
                                $"{System.IO.Path.GetFileNameWithoutExtension(filePath)} page {page + 1}{extension}"
                            );
                            image.Save(newFileName, ImageFormat.Tiff);
                            resultFiles.Add(newFileName);
                        }
                    }
                    else
                    {
                        resultFiles.Add(filePath);
                    }
                }
            }
            else
            {
                // Обычный файл-изображение
                resultFiles.Add(filePath);
            }
        }

        // Собираем обратно в строку
        return string.Join("; ", resultFiles);
    }




}
