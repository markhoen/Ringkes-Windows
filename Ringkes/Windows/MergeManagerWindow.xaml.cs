using Microsoft.Win32;
using Ringkes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ringkes
{
    public partial class MergeManagerWindow : Window
    {
        public ObservableCollection<PDFItem> Items
        {
            get;
        }

        public MergeManagerWindow(
            ObservableCollection<PDFItem> items)
        {
            InitializeComponent();

            Items = items;

            FileListView.ItemsSource = Items;

            RefreshList();
        }

        private Point startPoint;

        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is PDFItem item)
            {
                Items.Remove(item);

                RefreshList();
            }
        }

        private void FileListView_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void FileListView_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPos = e.GetPosition(null);

            if (Math.Abs(currentPos.X - startPoint.X) <
                SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPos.Y - startPoint.Y) <
                SystemParameters.MinimumVerticalDragDistance)
                return;

            ListViewItem item =
                FindAncestor<ListViewItem>(
                    (DependencyObject)e.OriginalSource);

            if (item == null)
                return;

            PDFItem data =
                (PDFItem)item.DataContext;

            DragDrop.DoDragDrop(
                item,
                data,
                DragDropEffects.Move);
        }

        private void FileListView_Drop(
            object sender,
            DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PDFItem)))
                return;

            PDFItem sourceItem =
                (PDFItem)e.Data.GetData(typeof(PDFItem));

            ListViewItem targetItem =
                FindAncestor<ListViewItem>(
                    (DependencyObject)e.OriginalSource);

            if (targetItem == null)
                return;

            PDFItem targetData =
                (PDFItem)targetItem.DataContext;

            int oldIndex =
                Items.IndexOf(sourceItem);

            int newIndex =
                Items.IndexOf(targetData);

            if (oldIndex == newIndex)
                return;

            Items.Move(oldIndex, newIndex);
        }

        private void MoveUp_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn.Tag is PDFItem item)
            {
                int index =
                    Items.IndexOf(item);

                if (index <= 0)
                    return;

                Items.Move(
                    index,
                    index - 1);
            }
        }

        private void MoveDown_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn.Tag is PDFItem item)
            {
                int index =
                    Items.IndexOf(item);

                if (index >= Items.Count - 1)
                    return;

                Items.Move(
                    index,
                    index + 1);
            }
        }

        private void Remove_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (FileListView.SelectedItem
                is PDFItem item)
            {
                Items.Remove(item);

                RefreshList();
            }
        }

        private void Clear_Click(
            object sender,
            RoutedEventArgs e)
        {
            Items.Clear();

            RefreshList();
        }

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void RefreshList()
        {
            bool isEmpty = Items.Count == 0;

            FileListView.Visibility =
                isEmpty
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            EmptyStatePanel.Visibility =
                isEmpty
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ReverseButton.IsEnabled =
                Items.Count > 1;
        }

        private bool AddPdfFile(
            string file)
        {
            if (!file.EndsWith(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool exists =
                Items.Any(x =>
                    string.Equals(
                        x.FilePath,
                        file,
                        StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return false;
            }

            Items.Add(
                new PDFItem
                {
                    FilePath = file
                });

            return true;
        }

        private void AddFiles_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog =
                new OpenFileDialog
                {
                    Filter =
                        "PDF Files (*.pdf)|*.pdf",
                    Multiselect = true
                };

            if (dialog.ShowDialog() == true)
            {
                int skipped = 0;

                foreach (string file
                         in dialog.FileNames)
                {
                    if (!AddPdfFile(file))
                    {
                        skipped++;
                    }
                }

                RefreshList();

                if (skipped == 1)
                {
                    MessageBox.Show(
                        "The selected PDF already exists in the merge list.",
                        "Duplicate File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (skipped > 1)
                {
                    MessageBox.Show(
                        $"{skipped} duplicate PDF files were ignored.",
                        "Duplicate Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void ReverseOrder_Click(
            object sender,
            RoutedEventArgs e)
        {
            List<PDFItem> reversed =
                Items
                .Reverse()
                .ToList();

            Items.Clear();

            foreach (PDFItem item in reversed)
            {
                Items.Add(item);
            }

            RefreshList();
        }

        private void Window_DragOver(
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

        private void Window_Drop(
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

            int skipped = 0;

            foreach (string file in files)
            {
                if (!AddPdfFile(file))
                {
                    skipped++;
                }
            }

            RefreshList();

            if (skipped == 1)
            {
                MessageBox.Show(
                    "The selected PDF already exists in the merge list.",
                    "Duplicate File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (skipped > 1)
            {
                MessageBox.Show(
                    $"{skipped} duplicate PDF files were ignored.",
                    "Duplicate Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private static T FindAncestor<T>(
            DependencyObject current)
            where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;

                current =
                    VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}