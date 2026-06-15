using Ringkes.Helpers;
using Ringkes.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Ringkes
{
    public partial class MainWindow : Window
    {
        ObservableCollection<PDFItem> pdfs =
        new ObservableCollection<PDFItem>();

        private readonly Queue<PDFItem> queue =
            new Queue<PDFItem>();

        private bool isProcessing =
            false;

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource = pdfs;
            LoadLogo();
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            int total =
                pdfs.Count;

            int finished =
                pdfs.Count(x =>
                    x.Status.StartsWith("Finished"));

            int waiting =
                queue.Count;

            FooterText.Text =
                string.Format(
                    "{0} file(s) • {1} completed • {2} queued",
                    total,
                    finished,
                    waiting);
        }

        private string FormatFileSize(
        long bytes)
        {
            string[] sizes =
            {
                "B",
                "KB",
                "MB",
                "GB"
            };

            double len = bytes;

            int order = 0;

            while (len >= 1024
                && order < sizes.Length - 1)
            {
                order++;

                len /= 1024;
            }

            return string.Format(
                "{0:0.#} {1}",
                len,
                sizes[order]);
        }
        private void ClearFinished_Click(
        object sender,
        RoutedEventArgs e)
        {
            for (int i = pdfs.Count - 1;
                i >= 0;
                i--)
            {
                if (pdfs[i]
                    .Status
                    .StartsWith("Finished"))
                {
                    pdfs.RemoveAt(i);
                }
            }
            UpdateFooter();
        }
        private async Task ProcessQueue()
        {
            isProcessing = true;
            while (queue.Count > 0)
            {
                PDFItem item =
                    queue.Dequeue();
                    await CompressPDF(item);
            }
            isProcessing = false;
        }
        private async Task EnqueueFile(PDFItem item)
        {
            queue.Enqueue(item);
            if (!isProcessing)
            {
                await ProcessQueue();
            }
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }
        private void DropArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }else{
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
                    UriKind.Relative
                );
            bitmap.EndInit();
            LogoImage.Source = bitmap;
        }
        private void DropArea_Drop( object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(
                DataFormats.FileDrop))
            {
                return;
            }
            string[] files =
                (string[])e.Data.GetData( DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (FileHelper.IsPDFFile(file))
                {
                    PDFItem item =
                        new PDFItem
                        {
                            FilePath = file,
                            OriginalPath = null,
                            Status = "Waiting",
                            Progress = 0,
                            IsTemporary = false
                        };
                    pdfs.Add(item);

                    UpdateFooter();

                    _ = EnqueueFile(item);
                }else if (FileHelper.IsImageFile(file)){
                    try
                    {
                        string tempPdf =
                            ImageToPdfService
                            .Convert(file);
                        PDFItem item =
                            new PDFItem
                            {
                                FilePath = tempPdf,
                                OriginalPath = file,
                                Status = "Converting...",
                                Progress = 0,
                                IsTemporary = true
                            };
                        pdfs.Add(item);

                        UpdateFooter();

                        _ = EnqueueFile(item);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            ex.Message);
                    }
                }
            }
        }
        private async Task CompressPDF(
            PDFItem item)
        {
            bool overwrite =
                OverwriteCheckBox
                .IsChecked == true;
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    item.Status =
                        "Preparing...";
                    item.Progress = 5;
                });
                Thread.Sleep(200);
                string inputFile =
                    item.FilePath;
                long originalSize =
                    new FileInfo(inputFile).Length;
                string namingSource =
                    item.OriginalPath
                    ?? inputFile;
                DateTime originalDate =
                    File.GetLastWriteTime(
                        inputFile);
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
                            Guid.NewGuid()
                            .ToString()
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
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        item.Status =
                            "Compressing PDF...";
                        item.Progress = 60;
                    });
                    int exitCode =
                        PdfCompressionService
                        .Compress(
                            inputFile,
                            outputFile);
                    if (exitCode == 0)
                    {
                        if (allowOverwrite)
                        {
                            string finalFile =
                                inputFile;
                            string backupFile =
                                finalFile
                                + ".backup";
                            try
                            {
                                Thread.Sleep(500);
                                if (File.Exists(
                                    backupFile))
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
                                    item.Progress =
                                        0;
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

                        long newSize;

                        if (allowOverwrite)
                        {
                            newSize =
                                new FileInfo(inputFile).Length;
                        }
                        else
                        {
                            newSize =
                                new FileInfo(outputFile).Length;
                        }

                        double savedPercent =
                            (1.0 -
                            ((double)newSize /
                            originalSize))
                            * 100.0;

                        string beforeSize =
                            FormatFileSize(
                                originalSize);

                        string afterSize =
                            FormatFileSize(
                                newSize);

                        string statusText;

                        if (savedPercent >= 0)
                        {
                            statusText =
                                string.Format(
                                    "Finished ({0} → {1}, {2:0.#}% smaller)",
                                    beforeSize,
                                    afterSize,
                                    savedPercent);
                        }
                        else
                        {
                            statusText =
                                string.Format(
                                    "Finished ({0} → {1}, {2:0.#}% larger)",
                                    beforeSize,
                                    afterSize,
                                    Math.Abs(savedPercent));
                        }

                        Dispatcher.Invoke(() =>
                        {
                            item.Progress = 100;
                            item.Status = statusText;

                            UpdateFooter();
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

                        UpdateFooter();
                    });
                }
            });
        }
    }

}