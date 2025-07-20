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
using MegaTextures.Previewer.Dialogs;
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;

namespace MegaTextures.Previewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class MainWindow : Window
    {
        [ObservableProperty]
        private MegaTexture? _currentTexture;

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

        partial void OnQuadVectorsChanged(QuadVectors value)
        {
            UpdateOutput();
        }

        partial void OnOutputSourceExpressionChanged(string? value)
        {
            UpdateOutput();
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

            CurrentTexture = new MegaTexture(
                TextureFormat.Bgra8888,
                newTextureDialog.TileWidth,
                newTextureDialog.TileHeight,
                newTextureDialog.Rows,
                newTextureDialog.Columns,
                newTextureDialog.Sources.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

            PresenterSource = CurrentTexture.Sources.First();
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
            UpdateOutput();
        }

        private void UpdateOutput()
        {
            if (CurrentOutput is null ||
                CurrentOutput.Width != OutputWidth ||
                CurrentOutput.Height != OutputHeight)
            {
                CurrentOutput = new WriteableBitmap(OutputWidth, OutputHeight, 96, 96, PixelFormats.Bgra32, null);
            }

            if (CurrentTexture is null)
            {
                ErrorMessage = "No MegaTexture";
                return;
            }

            if (OutputSourceExpression is null)
            {
                ErrorMessage = "Empty Source Expression";
                return;
            }

            MegaTexture.ExprSource source;
            try
            {
                source = CurrentTexture.CreateSource(OutputSourceExpression);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Invalid Source Expression. {ex.Message}";
                return;
            }

            var quad = QuadVectors;
            var buffer = new TextureData(TextureFormat.Bgra8888, OutputWidth, OutputHeight, CurrentOutput.BackBuffer, CurrentOutput.BackBufferStride);

            CurrentOutput.Lock();
            CurrentTexture.Read(source, quad, buffer);
            CurrentOutput.AddDirtyRect(new Int32Rect(0, 0, OutputWidth, OutputHeight));
            CurrentOutput.Unlock();
        }
    }
}