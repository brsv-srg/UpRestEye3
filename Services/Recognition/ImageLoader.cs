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
                    images.Add((LoadImageFromPdfUniversal(filePath), filePath));
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

    private Bitmap LoadImageFromPdfUniversal(string filePath)
    {
        //// 1. Пробуем извлечь вложенное изображение
        //try
        //{
        //    var img = LoadImageFromPdf(filePath);
        //    if (img != null)
        //        return img;
        //}
        //catch
        //{
        //    // Игнорируем, если не найдено изображение
        //}

        // 2. Если не получилось — рендерим страницу как изображение
        return LoadImageFromPdf2(filePath);
    }

    private Bitmap LoadImageFromPdf(string filePath)
    {
        using (iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filePath))
        {
            for (int pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
            {
                PdfDictionary pageDict = reader.GetPageN(pageNumber);
                PdfDictionary resources = (PdfDictionary)iTextSharp.text.pdf.PdfReader.GetPdfObject(pageDict.Get(PdfName.RESOURCES));
                PdfDictionary xObject = (PdfDictionary)iTextSharp.text.pdf.PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));

                if (xObject != null)
                {
                    foreach (PdfName name in xObject.Keys)
                    {
                        PdfObject obj = xObject.Get(name);
                        if (obj.IsIndirect())
                        {
                            PdfDictionary dict = (PdfDictionary)iTextSharp.text.pdf.PdfReader.GetPdfObject(obj);
                            PdfName subtype = (PdfName)iTextSharp.text.pdf.PdfReader.GetPdfObject(dict.Get(PdfName.SUBTYPE));

                            if (PdfName.IMAGE.Equals(subtype))
                            {
                                int xrefIndex = ((PRIndirectReference)obj).Number;
                                PdfObject pdfObj = reader.GetPdfObject(xrefIndex);
                                PdfStream pdfStream = (PdfStream)pdfObj;
                                byte[] bytes = iTextSharp.text.pdf.PdfReader.GetStreamBytesRaw((PRStream)pdfStream);

                                if (bytes != null)
                                {
                                    using (MemoryStream ms = new MemoryStream(bytes))
                                    {
                                        // Save the MemoryStream content to a file
                                        string outputFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), System.IO.Path.GetFileNameWithoutExtension(filePath) + ".png");

                                        // Check if the file already exists and delete it
                                        if (File.Exists(outputFilePath))
                                        {
                                            File.Delete(outputFilePath);
                                        }

                                        using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                                        {
                                            ms.WriteTo(fileStream);
                                        }

                                        // Set the file attributes to read-only
                                        //File.SetAttributes(outputFilePath, FileAttributes.ReadOnly);

                                        // Create Bitmap from the saved file

                                        using (var stream = new FileStream(outputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        {
                                            return new Bitmap(stream);
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
        throw new Exception("No images found in PDF.");
    }



    private Bitmap LoadImageFromPdf2(string filePath)
    {
        int dpi = 150;
        using (var rasterizer = new GhostscriptRasterizer())
        {
            rasterizer.Open(filePath);
            // Рендерим первую страницу (нумерация с 1)
            var img = rasterizer.GetPage(dpi, 1);
            return new Bitmap(img);
        }
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
}
