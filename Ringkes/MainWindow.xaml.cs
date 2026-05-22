using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

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
            e.Effects = DragDropEffects.Copy;
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

        private void DropArea_Drop(
            object sender,
            DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files =
                    (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower() == ".pdf")
                    {
                        PDFItem item = new PDFItem
                        {
                            FilePath = file,
                            Status = "Waiting",
                            Progress = 0
                        };

                        pdfs.Add(item);

                        _ = CompressPDF(item);
                    }
                }
            }
        }

        private async Task CompressPDF(PDFItem item)
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

                string inputFile = item.FilePath;

                string outputFile;

                if (overwrite)
                {
                    outputFile =
                        Path.Combine(
                            Path.GetDirectoryName(inputFile),
                            Guid.NewGuid().ToString() + ".pdf");
                }
                else
                {
                    outputFile =
                        Path.Combine(
                            Path.GetDirectoryName(inputFile),
                            Path.GetFileNameWithoutExtension(inputFile)
                            + ".ringkes.pdf");
                }

                string gsPath =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Tools",
                        "gswin64c.exe");

                if (!File.Exists(gsPath))
                {
                    Dispatcher.Invoke(() =>
                    {
                        item.Status = "Ghostscript not found";
                        item.Progress = 0;
                    });

                    return;
                }

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = gsPath;

                    process.StartInfo.Arguments =
                        "-sDEVICE=pdfwrite " +
                        "-dCompatibilityLevel=1.4 " +
                        "-dPDFSETTINGS=/ebook " +
                        "-dDetectDuplicateImages=true " +
                        "-dCompressFonts=true " +
                        "-dSubsetFonts=true " +
                        "-dNOPAUSE " +
                        "-dBATCH " +
                        "-sOutputFile=\"" + outputFile + "\" " +
                        "\"" + inputFile + "\"";

                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;

                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Status = "Launching Ghostscript...";
                            item.Progress = 20;
                        });

                        Thread.Sleep(200);

                        process.Start();

                        Dispatcher.Invoke(() =>
                        {
                            item.Status = "Compressing PDF...";
                            item.Progress = 60;
                        });

                        Thread.Sleep(200);

                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            if (overwrite)
                            {
                                File.Delete(inputFile);

                                File.Move(outputFile, inputFile);
                            }

                            Dispatcher.Invoke(() =>
                            {
                                item.Progress = 100;
                                item.Status = "Finished";
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                item.Progress = 0;
                                item.Status =
                                    "Ghostscript Failed (" +
                                    process.ExitCode +
                                    ")";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Progress = 0;
                            item.Status = ex.Message;
                        });
                    }
                }
            });
        }
    }
}