using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace Ringkes
{
    public partial class MainWindow : Window
    {
        ObservableCollection<PDFItem> pdfs =
            new ObservableCollection<PDFItem>();

        public MainWindow()
        {
            InitializeComponent();

            FileListView.ItemsSource = pdfs;

            LoadLogo();
        }

        private void About_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow about =
                new AboutWindow();

            about.Owner = this;

            about.ShowDialog();
        }

        private void DropArea_DragOver(
            object sender,
            DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void LoadLogo()
        {
            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();

            bitmap.UriSource =
                new Uri(
                    "Assets/logo.png",
                    UriKind.Relative);

            bitmap.EndInit();

            LogoImage.Source = bitmap;
        }

        private bool IsPDFFile(string path)
        {
            return Path.GetExtension(path)
                .ToLower() == ".pdf";
        }

        private bool IsImageFile(string path)
        {
            string ext =
                Path.GetExtension(path).ToLower();

            return ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".png"
                || ext == ".bmp"
                || ext == ".gif"
                || ext == ".webp";
        }

        private void DropArea_Drop(
            object sender,
            DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files =
                    (string[])e.Data.GetData(
                        DataFormats.FileDrop);

                foreach (string file in files)
                {
                    if (IsPDFFile(file))
                    {
                        PDFItem item = new PDFItem
                        {
                            FilePath = file,
                            OriginalPath = null,
                            Status = "Waiting",
                            Progress = 0,
                            IsTemporary = false
                        };

                        pdfs.Add(item);

                        _ = CompressPDF(item);
                    }
                    else if (IsImageFile(file))
                    {
                        ConvertImageToPDF(file);
                    }
                }
            }
        }

        private void ConvertImageToPDF(
            string imagePath)
        {
            try
            {
                string tempPdf =
                    Path.Combine(
                        Path.GetDirectoryName(imagePath),
                        Guid.NewGuid().ToString()
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

                PDFItem item = new PDFItem
                {
                    FilePath = tempPdf,
                    OriginalPath = imagePath,
                    Status = "Converting...",
                    Progress = 0,
                    IsTemporary = true
                };

                pdfs.Add(item);

                _ = CompressPDF(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task CompressPDF(
            PDFItem item)
        {
            bool overwrite =
                OverwriteCheckBox.IsChecked == true;

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    item.Status = "Preparing...";
                    item.Progress = 5;
                });

                Thread.Sleep(200);

                string inputFile =
                    item.FilePath;

                string namingSource =
                    item.OriginalPath
                    ?? inputFile;

                DateTime originalDate =
                    File.GetLastWriteTime(inputFile);

                bool isRealPDF =
                    item.OriginalPath == null;

                bool allowOverwrite =
                    overwrite && isRealPDF;

                string outputFile;

                if (allowOverwrite)
                {
                    outputFile =
                        Path.Combine(
                            Path.GetDirectoryName(
                                inputFile),
                            Guid.NewGuid().ToString()
                            + ".pdf");
                }
                else
                {
                    outputFile =
                        Path.Combine(
                            Path.GetDirectoryName(
                                namingSource),
                            Path.GetFileNameWithoutExtension(
                                namingSource)
                            + ".ringkes.pdf");
                }

                string gsPath =
                    Path.Combine(
                        AppDomain.CurrentDomain
                        .BaseDirectory,
                        "Tools",
                        "gswin64c.exe");

                if (!File.Exists(gsPath))
                {
                    Dispatcher.Invoke(() =>
                    {
                        item.Status =
                            "Ghostscript not found";

                        item.Progress = 0;
                    });

                    return;
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

                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Status =
                                "Launching Ghostscript...";

                            item.Progress = 20;
                        });

                        Thread.Sleep(200);

                        process.Start();

                        Dispatcher.Invoke(() =>
                        {
                            item.Status =
                                "Compressing PDF...";

                            item.Progress = 60;
                        });

                        Thread.Sleep(200);

                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            if (allowOverwrite)
                            {
                                string finalFile =
                                    inputFile;

                                string backupFile =
                                    finalFile + ".backup";

                                try
                                {
                                    Thread.Sleep(500);

                                    if (File.Exists(backupFile))
                                    {
                                        File.Delete(
                                            backupFile);
                                    }

                                    File.Copy(
                                        finalFile,
                                        backupFile,
                                        true);

                                    File.Delete(
                                        finalFile);

                                    File.Copy(
                                        outputFile,
                                        finalFile,
                                        true);

                                    File.Delete(
                                        outputFile);

                                    File.Delete(
                                        backupFile);
                                }
                                catch (Exception ex)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        item.Progress = 0;

                                        item.Status =
                                            "Overwrite failed: "
                                            + ex.Message;
                                    });

                                    return;
                                }

                                File.SetLastWriteTime(
                                    finalFile,
                                    originalDate);
                            }
                            else
                            {
                                if (!item.IsTemporary)
                                {
                                    File.SetLastWriteTime(
                                        outputFile,
                                        originalDate);
                                }
                            }

                            if (item.IsTemporary)
                            {
                                try
                                {
                                    File.Delete(
                                        inputFile);
                                }
                                catch
                                {
                                }
                            }

                            Dispatcher.Invoke(() =>
                            {
                                item.Progress = 100;

                                item.Status =
                                    "Finished";
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                item.Progress = 0;

                                item.Status =
                                    "Ghostscript Failed ("
                                    + process.ExitCode
                                    + ")";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Progress = 0;

                            item.Status =
                                ex.Message;
                        });
                    }
                }
            });
        }
    }
}