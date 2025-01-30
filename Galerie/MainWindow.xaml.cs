using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Drawing.Image;
using System.ComponentModel;
using System.Globalization;
using Color = System.Windows.Media.Color;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace Galerie
{
    public partial class MainWindow : Window
    {
        private bool isInitialized = false;
        private string selectedImagePath;
        private int imageSize = 100;
        private int fontSize = 12;

        public MainWindow()
        {
            InitializeComponent();
            isInitialized = true;
            LoadImages();
            DirectoryTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
        }

        private void LoadImages()
        {
            string directory = DirectoryTextBox.Text;

            if (Directory.Exists(directory))
            {
                ImagesWrapPanel.Children.Clear();

                // Složky
                DirectoryInfo parentDir = Directory.GetParent(directory);
                if (parentDir != null)
                {
                    Button upButton = new Button
                    {
                        Content = "⬆ Zpět",
                        Margin = new Thickness(5),
                        Padding = new Thickness(5),
                        Height = imageSize,
                        Width = imageSize
                    };
                    upButton.Click += (s, args) =>
                    {
                        DirectoryTextBox.Text = parentDir.FullName;
                        LoadImages();
                    };

                    ImagesWrapPanel.Children.Add(upButton);
                }

                try
                {
                    string[] directories = Directory.GetDirectories(directory);
                    foreach (string subDir in directories)
                    {
                        Button folderButton = new Button
                        {
                            Margin = new Thickness(5),
                            Padding = new Thickness(5),
                            Height = imageSize,
                            Width = imageSize,
                            Tag = subDir,
                            Content = new TextBlock
                            {
                                TextWrapping = System.Windows.TextWrapping.Wrap,
                                Text = System.IO.Path.GetFileName(subDir),
                                FontSize = fontSize,
                            }
                        };

                        folderButton.Click += (s, args) =>
                        {
                            DirectoryTextBox.Text = subDir;
                            LoadImages();
                        };

                        ImagesWrapPanel.Children.Add(folderButton);
                    }

                    // obrázky
                    string[] imageFiles = Directory.GetFiles(directory)
                                                   .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                                  file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                                                                  file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                                  file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                                                  file.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                                                                  file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                                   .ToArray();

                    switch (((ComboBoxItem)Order.SelectedItem).Content.ToString())
                    {
                        case "název vzestupně":
                            imageFiles = imageFiles.OrderBy(file => System.IO.Path.GetFileName(file)).ToArray();
                            break;

                        case "název sestupně":
                            imageFiles = imageFiles.OrderByDescending(file => System.IO.Path.GetFileName(file)).ToArray();
                            break;

                        case "čas úpravy vzestupně":
                            imageFiles = imageFiles.OrderBy(file => new FileInfo(file).LastWriteTime).ToArray();
                            break;

                        case "čas úpravy sestupně":
                            imageFiles = imageFiles.OrderByDescending(file => new FileInfo(file).LastWriteTime).ToArray();
                            break;

                        case "čas vytvoření vzestupně":
                            imageFiles = imageFiles.OrderBy(file => new FileInfo(file).CreationTime).ToArray();
                            break;

                        case "čas vytvoření sestupně":
                            imageFiles = imageFiles.OrderByDescending(file => new FileInfo(file).CreationTime).ToArray();
                            break;

                        case "velikost vzestupně":
                            imageFiles = imageFiles.OrderBy(file => new FileInfo(file).Length).ToArray();
                            break;

                        case "velikost sestupně":
                            imageFiles = imageFiles.OrderByDescending(file => new FileInfo(file).Length).ToArray();
                            break;
                    }

                    foreach (string file in imageFiles)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.DecodePixelWidth = imageSize;
                            bitmap.EndInit();
                        }

                        System.Windows.Controls.Image image = new System.Windows.Controls.Image
                        {
                            Source = bitmap,
                            Width = imageSize,
                            Height = imageSize,
                            Margin = new Thickness(3),
                            Tag = file
                        };
                        image.MouseDown += Image_MouseDown;

                        Border border = new Border
                        {
                            Child = image,
                            BorderBrush = Brushes.Transparent,
                            BorderThickness = new Thickness(2)
                        };

                        ImagesWrapPanel.Children.Add(border);
                    }
                }
                catch (System.UnauthorizedAccessException)
                {
                    MessageBox.Show($"Přístup odepřen", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {

                }
                
            }
            else
            {
                MessageBox.Show("Cesta neexistuje", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Order_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitialized && Order.SelectedItem is ComboBoxItem selectedItem)
            {
                LoadImages();
            }
        }

        private void DirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInitialized && Directory.Exists(DirectoryTextBox.Text))
            {
                LoadImages();
                DirectoryTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                DirectoryTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void Unselect_Click(object sender, RoutedEventArgs e)
        {
            MetadataTextBlock.Text = string.Empty;
            Preview.Source = null;
            selectedImagePath = null;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // resetuje border
            foreach (var child in ImagesWrapPanel.Children)
            {
                if (child is Border border)
                {
                    border.BorderBrush = Brushes.Transparent;
                }
            }

            if (sender is System.Windows.Controls.Image image && image.Tag is string filePath && image.Parent is Border parentBorder)
            {
                // přidá border a zobrazí metadata
                parentBorder.BorderBrush = Brushes.Red;
                selectedImagePath = filePath;

                FileInfo fileInfo = new FileInfo(filePath);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                string resolution = $"{bitmap.PixelWidth} x {bitmap.PixelHeight}";
                string dpi = $"{bitmap.DpiX} DPI";

                MetadataTextBlock.Text = $"Název: {fileInfo.Name}\n" +
                                         $"Velikost: {fileInfo.Length / 1024} KB\n" +
                                         $"Rozlišení: {resolution}\n" +
                                         $"DPI: {dpi}\n" +
                                         $"Datum vytvoření: {fileInfo.CreationTime}\n" +
                                         $"Datum změny: {fileInfo.LastWriteTime}";

                // náhled
                Preview.Source = new BitmapImage(new Uri(selectedImagePath));

                // dvojklik fullscreen
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                {
                    new ImageWindow(selectedImagePath).Show();
                }
            }
        }

        private void SelectPath_Click(object sender, RoutedEventArgs e) // kód ukraden od microsoftu
        {
            // Configure open folder dialog box
            Microsoft.Win32.OpenFolderDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = "Vyberte složku";

            // Show open folder dialog box
            bool? result = dialog.ShowDialog();

            // Process open folder dialog box results
            if (result == true)
            {
                // Get the selected folder
                DirectoryTextBox.Text = dialog.FolderName;
            }

            LoadImages();
        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            new Info().Show();
        }
        private bool IsSelected()
        {
            if (string.IsNullOrEmpty(selectedImagePath) || !File.Exists(selectedImagePath))
            {
                MessageBox.Show("Nejprve vyberte obrázek.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else
            {
                return true;
            }

        }
        private void UpdateImage()
        {
            // Aktualizace obrázku ve wpf okně
            foreach (var child in ImagesWrapPanel.Children)
            {
                if (child is Border border && border.Child is System.Windows.Controls.Image image &&
                    image.Tag is string path && path == selectedImagePath)
                {
                    BitmapImage updatedBitmap = new BitmapImage();
                    using (FileStream stream = new FileStream(selectedImagePath, FileMode.Open, FileAccess.Read))
                    {
                        updatedBitmap.BeginInit();
                        updatedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        updatedBitmap.StreamSource = stream;
                        updatedBitmap.EndInit();
                    }

                    image.Source = updatedBitmap;
                    break;
                }
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            File.Delete(selectedImagePath);

            Border borderToRemove = null;
            foreach (var child in ImagesWrapPanel.Children)
            {
                if (child is Border border && border.Child is System.Windows.Controls.Image image &&
                    image.Tag is string path && path == selectedImagePath)
                {
                    borderToRemove = border;
                    break;
                }
            }

            if (borderToRemove != null)
            {
                ImagesWrapPanel.Children.Remove(borderToRemove);
            }

            MetadataTextBlock.Text = string.Empty;
            Preview.Source = null;
            selectedImagePath = null;
        }
        private void Clone_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            string directory = System.IO.Path.GetDirectoryName(selectedImagePath);
            string originalFileName = System.IO.Path.GetFileNameWithoutExtension(selectedImagePath);
            string originalExtension = System.IO.Path.GetExtension(selectedImagePath);
            int index = 1;
            string newFileName = selectedImagePath;
            while (File.Exists(newFileName))
            {
                newFileName = System.IO.Path.Combine(directory, $"{originalFileName}({index}){originalExtension}");
                index++;
            }
                
            File.Copy(selectedImagePath,newFileName);
            AddImageToPanel(newFileName);
        }
        private void Rotate90_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                bitmap.Save(selectedImagePath);
            }
            UpdateImage();
        }
        private void RotateMinus90_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                bitmap.Save(selectedImagePath);
            }
            UpdateImage();
        }
        private void Rotate180_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                bitmap.Save(selectedImagePath);
            }
            UpdateImage();
        }
        private void FlipX_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                bitmap.Save(selectedImagePath);
            }
            UpdateImage();
        }
        private void FlipY_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bitmap.Save(selectedImagePath);
            }
            UpdateImage();
        }
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;

            // Configure printer dialog box
            var printDialog = new System.Windows.Controls.PrintDialog();

            // Show printer dialog box
            bool? result = printDialog.ShowDialog();

            if (result == true)
            {
                try
                {
                    // Load the selected image
                    BitmapImage bitmap = new BitmapImage();
                    using (FileStream stream = new FileStream(selectedImagePath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }

                    // Create an Image control to hold the bitmap
                    System.Windows.Controls.Image image = new System.Windows.Controls.Image
                    {
                        Source = bitmap,
                        Width = printDialog.PrintableAreaWidth,
                        Height = printDialog.PrintableAreaHeight,
                        Stretch = Stretch.Uniform
                    };

                    // Print the Image control
                    printDialog.PrintVisual(image, $"Printing: {Path.GetFileName(selectedImagePath)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při tisku: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelected())
                return;
            using (Bitmap bitmap = (Bitmap)Image.FromFile(selectedImagePath))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);
                        int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                        System.Drawing.Color grayColor = System.Drawing.Color.FromArgb(grayValue, grayValue, grayValue);
                        bitmap.SetPixel(x, y, grayColor);
                    }
                }

                string directory = System.IO.Path.GetDirectoryName(selectedImagePath);
                string originalFileName = System.IO.Path.GetFileName(selectedImagePath);
                string newFileName = System.IO.Path.Combine(directory, $"grayscale_{originalFileName}");
                bitmap.Save(newFileName);

                bool imageExists = false;
                foreach (var child in ImagesWrapPanel.Children)
                {
                    if (child is Border border && border.Child is System.Windows.Controls.Image image &&
                        image.Tag is string path && path == newFileName)
                    {
                        imageExists = true;
                        break;
                    }
                }

                if (!imageExists)
                {
                    AddImageToPanel(newFileName);
                }
                else
                {
                    UpdateImage();
                }
            }
        }

        private void AddImageToPanel(string filePath)
        {
            BitmapImage bitmap = new BitmapImage();
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.DecodePixelWidth = imageSize;
                bitmap.EndInit();
            }

            System.Windows.Controls.Image image = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Width = imageSize,
                Height = imageSize,
                Margin = new Thickness(3),
                Tag = filePath
            };
            image.MouseDown += Image_MouseDown;

            Border border = new Border
            {
                Child = image,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(2)
            };

            ImagesWrapPanel.Children.Add(border);
        }

        private void Small_Click(object sender, RoutedEventArgs e)
        {
            imageSize = 50;
            fontSize = 8;
            LoadImages();
        }
        private void Medium_Click(object sender, RoutedEventArgs e)
        {
            imageSize = 100;
            fontSize = 12;
            LoadImages();
        }
        private void Large_Click(object sender, RoutedEventArgs e)
        {
            imageSize = 155;
            fontSize = 14;
            LoadImages();
        }
        private void Huge_Click(object sender, RoutedEventArgs e)
        {
            imageSize = 210;
            fontSize = 16;
            LoadImages();
        }
    }
}