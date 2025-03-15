using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Runtime.InteropServices;
using ImageMagick;


public class ImageLoader
{

    public Bitmap LoadImage(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath).ToLower();

        switch (extension)
        {
            case ".pdf":
                return LoadImageFromPdf(filePath);
            case ".heic":
                return LoadImageFromHeic(filePath);
            default:
                return LoadImageFromFile(filePath);
        }
    }

    private Bitmap LoadImageFromPdf(string filePath)
    {
        using (PdfReader reader = new PdfReader(filePath))
        {
            for (int pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
            {
                PdfDictionary pageDict = reader.GetPageN(pageNumber);
                PdfDictionary resources = (PdfDictionary)PdfReader.GetPdfObject(pageDict.Get(PdfName.RESOURCES));
                PdfDictionary xObject = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));

                if (xObject != null)
                {
                    foreach (PdfName name in xObject.Keys)
                    {
                        PdfObject obj = xObject.Get(name);
                        if (obj.IsIndirect())
                        {
                            PdfDictionary dict = (PdfDictionary)PdfReader.GetPdfObject(obj);
                            PdfName subtype = (PdfName)PdfReader.GetPdfObject(dict.Get(PdfName.SUBTYPE));

                            if (PdfName.IMAGE.Equals(subtype))
                            {
                                int xrefIndex = ((PRIndirectReference)obj).Number;
                                PdfObject pdfObj = reader.GetPdfObject(xrefIndex);
                                PdfStream pdfStream = (PdfStream)pdfObj;
                                byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStream);

                                if (bytes != null)
                                {
                                    using (MemoryStream ms = new MemoryStream(bytes))
                                    {
                                        Bitmap img = new Bitmap(ms);
                                        // Return the first image found
                                        return img;
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

    private Bitmap LoadImageFromFile(string filePath)
    {
        return new Bitmap(filePath);
    }
}
