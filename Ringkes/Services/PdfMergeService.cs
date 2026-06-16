using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Collections.Generic;

namespace Ringkes.Services
{
    public static class PdfMergeService
    {
        public static void Merge(
            IEnumerable<string> files,
            string outputFile)
        {
            PdfDocument output =
                new PdfDocument();

            foreach (string file in files)
            {
                PdfDocument input =
                    PdfReader.Open(
                        file,
                        PdfDocumentOpenMode.Import);

                foreach (PdfPage page
                         in input.Pages)
                {
                    output.AddPage(page);
                }
            }

            output.Save(outputFile);
        }
    }
}