using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EleCho.AetherTex;
using AetherTex.Viewer.Dialogs;
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using System.IO;

namespace AetherTex.Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class MainWindow : Window
    {
        private OpenFileDialog? _openImageDialog;
        private SaveFileDialog? _saveImageDialog;

        [ObservableProperty]
        private AetherTexImage? _currentTexture;

        [ObservableProperty]
        private string? _presenterSource;

        [ObservableProperty]
        private string? _outputSourceExpression;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _outputWidth = 1024;

        [ObservableProperty]
        private int _outputHeight = 1024;

        [ObservableProperty]
        private QuadVectors _quadVectors;

        [ObservableProperty]
        private WriteableBitmap? _currentOutput;

        public string[] PredefinedSources { get; } = new string[]
        {
            "side",
            "top"
        };

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private static TextureData GetTextureData(SKBitmap bitmap)
        {
            return new TextureData(TextureFormat.Bgra8888, bitmap.Width, bitmap.Height, bitmap.GetPixels(), bitmap.RowBytes);
        }

        private void SetCurrentImage(AetherTexImage image)
        {
            CurrentTexture = image;
            PresenterSource = image.Sources.First();
            OutputSourceExpression = PresenterSource;

            imageViewer.UpdateImage();
        }

        private void NewMegaTexture_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var newTextureDialog = new NewTextureDialog()
            {
                Owner = this,
            };

            if (newTextureDialog.ShowDialog() != true)
            {
                return;
            }

            SetCurrentImage(new AetherTexImage(
                TextureFormat.Bgra8888,
                newTextureDialog.TileWidth,
                newTextureDialog.TileHeight,
                newTextureDialog.Rows,
                newTextureDialog.Columns,
                newTextureDialog.Sources.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)));
        }

        private void ImportImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTexture is null)
            {
                MessageBox.Show(this, "Error", "No MegaTexture created", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var importImageDialog = new ImportImageDialog(CurrentTexture)
            {
                Owner = this,
            };

            if (importImageDialog.ShowDialog() != true)
            {
                return;
            }

            var bitmap = SKBitmap.Decode(importImageDialog.FilePath);
            CurrentTexture.Write(GetTextureData(bitmap), importImageDialog.TargetSource, importImageDialog.Column, importImageDialog.Row);
            megaTexturePresenter.UpdateTileImage(importImageDialog.Column, importImageDialog.Row);
        }

        private void OpenImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _openImageDialog ??= new OpenFileDialog()
            {
                Title = "Open Image",
                Filter = "AetherTex Image|*.atimg",
                CheckFileExists = true,
            };

            if (_openImageDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                using var file = File.OpenRead(_openImageDialog.FileName);
                var image = AetherTexImage.Deserialize(file);

                SetCurrentImage(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to open", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTexture is null)
            {
                return;
            }

            _saveImageDialog ??= new SaveFileDialog()
            {
                Title = "Save Image",
                Filter = "AetherTex Image|*.atimg",
                DefaultExt = ".atimg",
            };

            if (_saveImageDialog.ShowDialog(this) != true)
            {
                return;
            }

            using var file = File.Create(_saveImageDialog.FileName);
            try
            {
                AetherTexImage.Serialize(CurrentTexture, file);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}