using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Ringkes.Services
{
    public static class PdfMergeService
    {
        public static bool Merge(
            string[] files,
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

                foreach (PdfPage page in input.Pages)
                {
                    output.AddPage(page);
                }
            }

            output.Save(outputFile);

            return true;
        }
    }
}