using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Ringkes.Services
{
    public static class ImageToPdfService
    {
        public static string Convert(string imagePath)
        {
            string tempPdf =
                Path.Combine(
                    Path.GetDirectoryName(imagePath),
                    Guid.NewGuid()
                    + ".pdf");

            PdfDocument document =
                new PdfDocument();

            PdfPage page =
                document.AddPage();

            using (XImage img =
                XImage.FromFile(imagePath))
            {
                page.Width =
                    XUnit.FromPoint(
                        img.PixelWidth);

                page.Height =
                    XUnit.FromPoint(
                        img.PixelHeight);

                using (XGraphics gfx =
                    XGraphics.FromPdfPage(page))
                {
                    gfx.DrawImage(
                        img,
                        0,
                        0,
                        img.PixelWidth,
                        img.PixelHeight);
                }
            }

            document.Save(tempPdf);

            return tempPdf;
        }
    }
}