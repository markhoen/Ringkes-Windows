using Microsoft.Win32;
using Ringkes.Helpers;
using Ringkes.Models;
using Ringkes.Services;
using Ringkes.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ringkes
{
    public partial class MainWindow : Window
    {
        ObservableCollection<PDFItem> pdfs =
            new ObservableCollection<PDFItem>();

        private readonly Queue<PDFItem> queue =
            new Queue<PDFItem>();

        private readonly ObservableCollection<MergeHistoryItem> mergeHistory =
            new ObservableCollection<MergeHistoryItem>();

        private bool isProcessing =
            false;

        private RingkesMode currentMode = 
            RingkesMode.Compress;

        private readonly ObservableCollection<PDFItem>
            mergeItems =
                new ObservableCollection<PDFItem>();

        public bool HasMergeFiles =>
            mergeItems.Count > 0;

        public bool CanMerge =>
            mergeItems.Count > 1;

        public MainWindow()
        {
            InitializeComponent();

            CheckGhostscript();

            UpdateModeUI();
            LoadLogo();
            UpdateFooter();

            FileListView.ItemsSource = pdfs;
            MergeHistoryListView.ItemsSource = mergeHistory;
        }

        private bool ghostscriptAvailable;

        private void CheckGhostscript()
        {
            string gsPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Tools",
                    "gswin64c.exe");

            if (!File.Exists(gsPath))
            {
                GhostscriptStatusText.Text =
                    "Ghostscript Not Found";

                GhostscriptIndicator.Fill = 
                    Brushes.Firebrick;

                GhostscriptPathText.Text =
                    "Compression mode unavailable";

                CompressModeButton.IsEnabled = false;

                ghostscriptAvailable = false;

                return;
            }

            try
            {
                FileVersionInfo info =
                    FileVersionInfo.GetVersionInfo(gsPath);

                string version =
                    info.FileVersion;

                GhostscriptStatusText.Text =
                    "Ghostscript Ready";

                GhostscriptIndicator.Fill =
                    Brushes.LimeGreen;

                GhostscriptPathText.Text =
                    $"Embedded Engine • v{version}";

                ghostscriptAvailable = true;
            }
            catch
            {
                GhostscriptStatusText.Text =
                    "Ghostscript Detected";

                GhostscriptIndicator.Fill = 
                    Brushes.OrangeRed;

                GhostscriptPathText.Text =
                    "Version information unavailable";

                ghostscriptAvailable = true;
            }
        }

        private void CompressModeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentMode = RingkesMode.Compress;

            FileListView.Visibility = Visibility.Visible;

            MergeHistoryListView.Visibility = Visibility.Collapsed;

            UpdateModeUI();
        }

        private void MergeModeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentMode = RingkesMode.Merge;

            FileListView.Visibility = Visibility.Collapsed;

            MergeHistoryListView.Visibility = Visibility.Visible;

            UpdateModeUI();
        }

        private void OpenMergedFile_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            if (!(element.Tag is string path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    "File not found.",
                    "Ringkes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Process.Start(
                new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
        }

        private void OpenMergedFolder_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            if (!(element.Tag is string path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    "File not found.",
                    "Ringkes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Process.Start(
                "explorer.exe",
                $"/select,\"{path}\"");
        }

        private void UpdateModeUI()
        {
            if (currentMode == RingkesMode.Compress)
            {
                CompressModeButton.Background =
                    Brushes.DodgerBlue;

                CompressModeButton.Foreground =
                    Brushes.White;

                MergeModeButton.Background =
                    Brushes.White;

                MergeModeButton.Foreground =
                    Brushes.Black;

                DropIconText.Text = "📄";

                DropTitleText.Text =
                    "Drop PDF or Images";

                DropSubtitleText.Text =
                    "COMPRESS PDF FILES";

                GhostscriptPanel.Visibility =
                    Visibility.Visible;

                FileListView.Visibility =
                    Visibility.Visible;

                ClearFinishedButton.Visibility =
                    Visibility.Visible;

                ClearFinishedButton.IsEnabled =
                    pdfs.Any(x =>
                        x.Status.StartsWith("Finished"));

                ManageFilesButton.Visibility =
                    Visibility.Collapsed;

                MergeButton.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                MergeModeButton.Background =
                    Brushes.DodgerBlue;

                MergeModeButton.Foreground =
                    Brushes.White;

                CompressModeButton.Background =
                    Brushes.White;

                CompressModeButton.Foreground =
                    Brushes.Black;

                DropIconText.Text = "📚";

                DropTitleText.Text =
                    "Drop PDFs to Merge";

                DropSubtitleText.Text =
                    "COMBINE MULTIPLE PDF FILES";

                GhostscriptPanel.Visibility =
                    Visibility.Collapsed;

                FileListView.Visibility =
                    Visibility.Collapsed;

                ClearFinishedButton.Visibility =
                    Visibility.Collapsed;

                ManageFilesButton.Visibility =
                    Visibility.Visible;

                MergeButton.Visibility =
                    Visibility.Visible;

                RefreshMergeUI();
            }

            UpdateFooter();
        }

        private void ManageFilesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MergeManagerWindow wnd =
                new MergeManagerWindow(
                    mergeItems);

            wnd.Owner = this;

            wnd.ShowDialog();

            RefreshMergeUI();
        }

        private async void MergeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (mergeItems.Count < 2)
            {
                return;
            }

            SaveFileDialog dialog =
                new SaveFileDialog
                {
                    Filter =
                        "PDF Files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    AddExtension = true,
                    FileName = ""
                };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string selectedPath = dialog.FileName;

            string directory =
                Path.GetDirectoryName(selectedPath) ?? "";

            string filename =
                Path.GetFileNameWithoutExtension(selectedPath);

            if (string.IsNullOrWhiteSpace(filename))
            {
                MessageBox.Show(
                    "Please enter a file name.",
                    "Missing File Name",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string finalOutputPath =
                Path.Combine(
                    directory,
                    $"{filename}_merged_by_Ringkes_{DateTime.Now:yyyyMMdd}.pdf");

            MergeProgressWindow progress = null;

            try
            {
                progress =
                    new MergeProgressWindow(
                        finalOutputPath);

                progress.Owner = this;

                progress.Show();

                await Task.Run(() =>
                {
                    PdfMergeService.Merge(
                        mergeItems.Select(
                            x => x.FilePath),
                        finalOutputPath);
                });

                mergeHistory.Insert(
                    0,new MergeHistoryItem
                {
                    FileName =
                        Path.GetFileName(
                            finalOutputPath),

                    OutputPath =
                        finalOutputPath,

                    SourceCount =
                        mergeItems.Count,

                    CreatedAt =
                        DateTime.Now
                });

                FooterText.Text =
                    $"{mergeHistory.Count} merge history item(s)";

                MessageBox.Show(
                    $"PDF files merged successfully.\n\n{Path.GetFileName(finalOutputPath)}",
                    "Merge Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Merge Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (progress != null)
                    progress.Close();
            }
        }

        private void UpdateFooter()
        {
            ClearFinishedButton.IsEnabled =
                pdfs.Any(x =>
                    x.Status.StartsWith("Finished"));

            if (currentMode ==
                RingkesMode.Merge)
            {
                RefreshMergeUI();
                return;
            }

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

        private void HandleCompressDrop(
            string[] files)
        {
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
                }
                else if (FileHelper.IsImageFile(file))
                {
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

        private void HandleMergeDrop(
            string[] files)
        {
            foreach (string file in files)
            {
                if (!FileHelper.IsPDFFile(file))
                {
                    continue;
                }

                if (mergeItems.Any(
                    x => x.FilePath == file))
                {
                    continue;
                }

                mergeItems.Add(
                    new PDFItem
                    {
                        FilePath = file,
                        Status = "Ready"
                    });
            }

            RefreshMergeUI();
        }

        private void RefreshMergeUI()
        {
            if (currentMode !=
                RingkesMode.Merge)
            {
                return;
            }

            if (mergeItems.Count == 0)
            {
                FooterText.Text =
                    "No PDF files selected";
            }
            else
            {
                FooterText.Text =
                    $"{mergeItems.Count} PDF file(s) selected";
            }

            ManageFilesButton.IsEnabled =
                mergeItems.Count > 0;

            MergeButton.IsEnabled =
                mergeItems.Count > 1;

            DropTitleText.Text =
                mergeItems.Count == 0
                ? "Drop PDFs to Merge"
                : $"{mergeItems.Count} file(s) in merge queue";
        }

        private void DropArea_Drop(
            object sender,
            DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(
                DataFormats.FileDrop))
            {
                return;
            }

            string[] files =
                (string[])e.Data.GetData(
                    DataFormats.FileDrop);

            if (currentMode ==
                RingkesMode.Compress)
            {
                HandleCompressDrop(files);
            }
            else
            {
                HandleMergeDrop(files);
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