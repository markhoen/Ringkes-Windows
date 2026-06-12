using System;
using System.Diagnostics;
using System.IO;

namespace Ringkes.Services
{
    public static class PdfCompressionService
    {
        public static int Compress(
            string inputFile,
            string outputFile)
        {
            string gsPath =
                Path.Combine(
                    AppDomain.CurrentDomain
                    .BaseDirectory,
                    "Tools",
                    "gswin64c.exe");

            if (!File.Exists(gsPath))
            {
                throw new FileNotFoundException(
                    "Ghostscript not found",
                    gsPath);
            }

            using (Process process =
                new Process())
            {
                process.StartInfo.FileName =
                    gsPath;

                process.StartInfo.Arguments =
                    "-sDEVICE=pdfwrite " +
                    "-dCompatibilityLevel=1.4 " +
                    "-dPDFSETTINGS=/ebook " +
                    "-dDetectDuplicateImages=true " +
                    "-dCompressFonts=true " +
                    "-dSubsetFonts=true " +
                    "-dNOPAUSE " +
                    "-dBATCH " +
                    "-sOutputFile=\"" +
                    outputFile + "\" " +
                    "\"" + inputFile + "\"";

                process.StartInfo
                    .CreateNoWindow = true;

                process.StartInfo
                    .UseShellExecute = false;

                process.Start();

                process.WaitForExit();

                return process.ExitCode;
            }
        }
    }
}